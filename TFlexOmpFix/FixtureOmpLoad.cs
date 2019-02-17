﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TFlex.Model;
using TFlexOmpFix.Exceptions;
using TFlexOmpFix.Objects;
using TFlexOmpFix.Procedure;
using TFlexOmpFix.Repositories;

namespace TFlexOmpFix
{
    public enum ObjTypes
    {
        Document = 0, Specification = 1, Detail = 2, SpecDraw = 6,
        UserDocument = 20, Complect = 22, SpecFixture = 31
    }

    public enum Sections
    {
        Documentation = 0, Specification = 1, Detail = 2, Complect = 22
    }

    public sealed class FixtureOmpLoad
    {
        private ILogging iLog;

        private readonly Document doc;

        private Settings settings;

        private decimal ompUserCode, ownerCode;

        private List<Document> stackDocs;

        private void ExportInizialize()
        {
            // Пользователь в КИС "Омега"
            UserRepository userRep = new UserRepository();
            ompUserCode = userRep.GetUser(settings.UserName);

            // Владелец
            OwnerRepository ownerRep = new OwnerRepository();
            ownerCode = ownerRep.GetOwnerByUser(ompUserCode);

            // открытые документы
            stackDocs = new List<Document>();
        }

        private void LogElement(ElementData elemData)
        {
            StringBuilder rowText = new StringBuilder();

            rowText.Append(elemData.Section);
            rowText.Append(" ");
            rowText.Append(elemData.Position);
            rowText.Append(" ");
            rowText.Append(elemData.Sign);
            rowText.Append(" ");
            rowText.Append(elemData.Name);
            rowText.Append(" ");
            rowText.Append(elemData.Qty);
            rowText.Append(" ");
            rowText.Append(elemData.DocCode);
            rowText.Append(" ");
            rowText.Append(elemData.FilePath);

            iLog.Write("* " + rowText.ToString());
        }

        private void ExportDoc(Document doc, string configuration = null)
        {
            // валидность структуры
            IsValid(doc, configuration);

            // структура изделия
            ProductStructure structure;

            if (doc.ModelConfigurations.ConfigurationCount == 0 || configuration == null)
            {
                structure = doc
                    .GetProductStructures()
                    .FirstOrDefault();
            }
            else
            {
                structure = doc
                    .GetProductStructures()
                    .Where(x => x.Name == configuration)
                    .FirstOrDefault();
            }

            // схема параметров
            SchemeDataConfig schemeConfig = new SchemeDataConfig(structure);
            SchemeData scheme = schemeConfig.GetScheme();

            // родительский элемент
            RowElement parentElem = structure.GetAllRowElements().
                Where(x => x.ParentRowElement == null).FirstOrDefault();

            ElementDataConfig dataConfig = new ElementDataConfig(parentElem, scheme);
            ElementData elemData = dataConfig.ConfigData();

            // логирование
            LogElement(elemData);

            // проверка обозначения у родителя
            if (elemData.Sign == String.Empty || elemData.Section == String.Empty)
                throw new DocStructurePerentPropsException();

            // команды
            TFlexObjSynchRepository synchRep = new TFlexObjSynchRepository();

            // процедуры
            CreateSpecFix prSpec = new CreateSpecFix();
            CreateSpecDraw prSpecDraw = new CreateSpecDraw();
            AddFile prFile = new AddFile();
            ClearSpecification prClear = new ClearSpecification();
            AddElement prAddElem = new AddElement();
            CreateDocument prDocument = new CreateDocument();

            // родительский элемент
            decimal? parent = null;

            // код БД
            decimal code = 0;
            decimal doccode = 0;

            // синхронизация с КИС "Омега"
            V_SEPO_TFLEX_OBJ_SYNCH synchObj = synchRep.GetSynchObj(elemData.MainSection, elemData.DocCode);

            // если головной элемент не синхронизирован - выход
            if (synchObj == null) return;

            #region экспорт родителя

            switch (synchObj.KOTYPE)
            {
                case (decimal)ObjTypes.SpecFixture:

                    // создание спецификации
                    prSpec.Exec(
                        elemData.Sign,
                        elemData.Name,
                        ownerCode,
                        synchObj.BOSTATECODE,
                        ompUserCode,
                        ref code);

                    // очищение спецификации
                    prClear.Exec(code);

                    // родительский элемент
                    parent = code;

                    // поиск файла спецификации по шаблону
                    string signPattern = Regex.Replace(elemData.Sign, @"\D", "");

                    string[] files = Directory.GetFiles(doc.FilePath);

                    foreach (var file in files)
                    {
                        FileInfo fileInfo = new FileInfo(file);

                        string filePattern = Regex.Replace(fileInfo.Name, @"\D", "");
                        if (filePattern == signPattern && file.Contains("СП"))
                        {
                            prFile.Exec(code, file, ompUserCode, synchObj.FILEGROUP, null, ref doccode);
                        }
                    }

                    break;

                case (decimal)ObjTypes.SpecDraw:

                    // создание сборочного чертежа
                    prSpecDraw.Exec(
                        elemData.Sign,
                        elemData.Name,
                        ownerCode,
                        synchObj.BOSTATECODE,
                        ompUserCode,
                        ref code);

                    // присоединенный файл
                    prFile.Exec(code, doc.FileName, ompUserCode, synchObj.FILEGROUP, null, ref doccode);

                    break;

                case (decimal)ObjTypes.Document:
                case (decimal)ObjTypes.UserDocument:

                    // создание документа
                    prDocument.Exec(
                        synchObj.BOTYPE,
                        elemData.Sign,
                        elemData.Name,
                        ownerCode,
                        synchObj.BOSTATECODE,
                        ompUserCode,
                        ref code);

                    // присоединенный файл
                    prFile.Exec(code, doc.FileName, ompUserCode, synchObj.FILEGROUP, null, ref doccode);

                    break;

                case (decimal)ObjTypes.Detail:

                    // создание детали
                    CreateDetail pd = new CreateDetail();
                    pd.Exec(
                        elemData.Sign,
                        elemData.Name,
                        ownerCode,
                        synchObj.BOSTATECODE,
                        ompUserCode,
                        ref code);

                    // присоединенный файл
                    prFile.Exec(code, doc.FileName, ompUserCode, synchObj.FILEGROUP, null, ref doccode);

                    // модели для детали
                    var models = structure.GetAllRowElements().Where(x => x.ParentRowElement == parentElem);

                    foreach (var model in models)
                    {
                        ElementDataConfig modelDataConfig = new ElementDataConfig(model, scheme);
                        ElementData modelData = modelDataConfig.ConfigData();

                        if (modelData.FilePath != null)
                        {
                            try
                            {
                                decimal linkdoccode = 0;

                                prFile.Exec(code, modelData.FilePath, ompUserCode, synchObj.FILEGROUP, doccode, ref linkdoccode);
                            }
                            catch (FileNotFoundException)
                            {
                                iLog.Write("ОШИБКА! Файл " + elemData.FilePath + " не найден!");
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                        }
                    }

                    // моделями также являются невидимые фрагменты у деталей
                    foreach (var fragment in doc.GetFragments().Where(x => !x.Visible))
                    {
                        if (fragment.FullFilePath != null)
                        {
                            try
                            {
                                decimal linkdoccode = 0;

                                prFile.Exec(code, fragment.FullFilePath, ompUserCode, synchObj.FILEGROUP, doccode, ref linkdoccode);
                            }
                            catch (FileNotFoundException)
                            {
                                iLog.Write("ОШИБКА! Файл " + fragment.FullFilePath + " не найден!");
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                        }
                    }

                    break;

                default:
                    break;
            }

            #endregion экспорт родителя

            #region элементы спецификации

            if (elemData.MainSection == "Сборочные единицы")
            {
                foreach (var elem in
                    structure
                    .GetAllRowElements()
                    .Where(x => x.ParentRowElement == parentElem)
                    )
                {
                    // получение данных о документе
                    dataConfig = new ElementDataConfig(elem, scheme);

                    elemData = dataConfig.ConfigData();

                    LogElement(elemData);

                    // если обозначение или секция пустые, то переход на следующий элемент
                    if (elemData.Sign == String.Empty || elemData.Section == String.Empty) continue;

                    // синхронизация с КИС "Омега"
                    synchObj = synchRep.GetSynchObj(elemData.MainSection, elemData.DocCode);

                    // переход на следующий элемент, если позиция не синхронизирована
                    if (synchObj == null) continue;

                    switch (synchObj.KOTYPE)
                    {
                        case (decimal)ObjTypes.SpecFixture:

                            // создание спецификации
                            prSpec.Exec(
                                elemData.Sign,
                                elemData.Name,
                                ownerCode,
                                synchObj.BOSTATECODE,
                                ompUserCode,
                                ref code);

                            // очищение спецификации
                            prClear.Exec(code);

                            // если у сборки есть фрагмент...
                            if (elemData.FilePath != null)
                            {
                                try
                                {
                                    if (!File.Exists(elemData.FilePath)) throw new FileNotFoundException();

                                    // открыть документ входящей сборки в режиме чтения
                                    Document linkDoc = TFlex.Application.OpenDocument(elemData.FilePath, false, true);

                                    // добавить документ в стек
                                    stackDocs.Add(linkDoc);

                                    // экспорт входящей сборки
                                    ExportDoc(linkDoc, elemData.Sign);

                                    // закрытие документа
                                    linkDoc.Close();

                                    // удалить из стека документ
                                    stackDocs.Remove(linkDoc);
                                }
                                catch (FileNotFoundException)
                                {
                                    iLog.Write("ОШИБКА! Файл " + elemData.FilePath + " не найден!");
                                }
                                catch (Exception)
                                {
                                    throw;
                                }
                            }

                            break;

                        case (decimal)ObjTypes.SpecDraw:

                            // создание спецификации
                            prSpecDraw.Exec(
                                elemData.Sign,
                                elemData.Name,
                                ownerCode,
                                synchObj.BOSTATECODE,
                                ompUserCode,
                                ref code);

                            // присоединенный файл
                            prFile.Exec(code, doc.FileName, ompUserCode, synchObj.FILEGROUP, null, ref doccode);

                            break;

                        case (decimal)ObjTypes.Document:
                        case (decimal)ObjTypes.UserDocument:

                            prDocument.Exec(
                                synchObj.BOTYPE,
                                elemData.Sign,
                                elemData.Name,
                                ownerCode,
                                synchObj.BOSTATECODE,
                                ompUserCode,
                                ref code);

                            // файл
                            if (elemData.FilePath != null)
                            {
                                try
                                {
                                    prFile.Exec(code, elemData.FilePath, ompUserCode, synchObj.FILEGROUP, null, ref doccode);
                                }
                                catch (FileNotFoundException)
                                {
                                    iLog.Write("ОШИБКА! Файл " + elemData.FilePath + " не найден!");
                                }
                                catch (Exception)
                                {
                                    throw;
                                }
                            }

                            break;

                        case (decimal)ObjTypes.Detail:

                            // создание детали
                            CreateDetail pd = new CreateDetail();
                            pd.Exec(
                                elemData.Sign,
                                elemData.Name,
                                ownerCode,
                                synchObj.BOSTATECODE,
                                ompUserCode,
                                ref code);

                            // файл
                            if (elemData.FilePath != null)
                            {
                                try
                                {
                                    prFile.Exec(code, elemData.FilePath, ompUserCode, synchObj.FILEGROUP, null, ref doccode);
                                }
                                catch (FileNotFoundException)
                                {
                                    doccode = 0;
                                    iLog.Write("ОШИБКА! Файл " + elemData.FilePath + " не найден!");
                                }
                                catch (Exception)
                                {
                                    doccode = 0;
                                    throw;
                                }
                            }

                            // модели для детали
                            var models = structure.GetAllRowElements().Where(x => x.ParentRowElement == elem);

                            foreach (var model in models)
                            {
                                ElementDataConfig modelDataConfig = new ElementDataConfig(model, scheme);
                                ElementData modelData = modelDataConfig.ConfigData();

                                if (modelData.FilePath != null)
                                {
                                    try
                                    {
                                        decimal linkdoccode = 0;

                                        prFile.Exec(code, modelData.FilePath, ompUserCode, synchObj.FILEGROUP,
                                            (doccode == 0) ? null : (decimal?)doccode, ref linkdoccode);
                                    }
                                    catch (FileNotFoundException)
                                    {
                                        iLog.Write("ОШИБКА! Файл " + elemData.FilePath + " не найден!");
                                    }
                                    catch (Exception)
                                    {
                                        throw;
                                    }
                                }
                            }

                            // открывается документ на деталь, если есть
                            if (elemData.FilePath != null)
                            {
                                try
                                {
                                    if (!File.Exists(elemData.FilePath)) throw new FileNotFoundException();

                                    // открыть документ входящей сборки в режиме чтения
                                    Document linkDoc = TFlex.Application.OpenDocument(elemData.FilePath, false, true);

                                    // добавить документ в стек
                                    stackDocs.Add(linkDoc);

                                    // экспорт детали
                                    ExportDoc(linkDoc, elemData.Sign);

                                    // закрытие документа
                                    linkDoc.Close();

                                    // удалить из стека документ
                                    stackDocs.Remove(linkDoc);
                                }
                                catch (FileNotFoundException)
                                {
                                    iLog.Write("ОШИБКА! Файл " + elemData.FilePath + " не найден!");
                                }
                                catch (Exception)
                                {
                                    throw;
                                }
                            }

                            break;

                        default:
                            code = 0;
                            break;
                    }

                    // добавить очередной элемент в спецификацию
                    if (code != 0)
                    {
                        prAddElem.Exec(
                            parent.Value,
                            code,
                            synchObj.KOTYPE,
                            synchObj.OMPSECTION,
                            elemData.Qty.Value,
                            elemData.Position,
                            ompUserCode);
                    }
                }
            }

            #endregion элементы спецификации
        }

        public FixtureOmpLoad(Settings settings, ILogging iLog, Document doc)
        {
            this.settings = settings;
            this.iLog = iLog;
            this.doc = doc;
        }

        /// <summary>
        /// Верификации документа
        /// </summary>
        /// <param name="doc">Документ</param>
        /// <returns></returns>
        public void IsValid(Document doc, string configuration = null)
        {
            // существование единственной структуры
            DocStructureSingularHandler singHandler = new DocStructureSingularHandler();

            // существование родителя
            DocStructureParentHandler parentHandler = new DocStructureParentHandler();

            // проверка обозначения родителя на валидность
            DocStructureParentNotValidHandler parentSignHandler = new DocStructureParentNotValidHandler();

            // проверка типа документа у родителя
            DocStructureDocTypeNotValidHandler parentDocTypeHandler = new DocStructureDocTypeNotValidHandler();

            singHandler.Next = parentHandler;
            parentHandler.Next = parentSignHandler;
            parentSignHandler.Next = parentDocTypeHandler;

            singHandler.IsValid(doc, configuration);
        }

        public void Export()
        {
            try
            {
                ExportInizialize();
                ExportDoc(doc);
            }
            catch (Exception)
            {
                // при возникновении ошибки закрыть все дочерние документы
                foreach (var document in stackDocs)
                {
                    document.Close();
                }

                stackDocs.Clear();

                throw;
            }
        }
    }
}