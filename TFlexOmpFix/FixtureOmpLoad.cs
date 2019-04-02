using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        UserDocument = 20, Complect = 22, SpecFixture = 31, Fixture = 32
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

        private decimal ompUserCode, ownerCode, fixTypeCode;

        private List<Document> stackDocs;

        private Stopwatch sw;

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
            iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName, elemData.Section, elemData.Position,
                elemData.Sign, elemData.Name, elemData.Qty, elemData.DocCode, elemData.FilePath, null);

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
            V_SEPO_TFLEX_OBJ_SYNCH synchObj = synchRep.GetSynchObj(elemData.MainSection, elemData.DocCode, elemData.Sign);

            // если головной элемент не синхронизирован - выход
            if (synchObj == null) return;

            #region экспорт родителя

            switch (synchObj.KOTYPE)
            {
                case (decimal)ObjTypes.SpecFixture:

                    #region Спецификация оснастки

                    prSpec.Exec(
                        elemData.Sign,
                        elemData.Name,
                        ownerCode,
                        synchObj.BOSTATECODE,
                        ompUserCode,
                        fixTypeCode,
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

                    #endregion Спецификация оснастки

                    break;

                case (decimal)ObjTypes.SpecDraw:

                    #region Сборочный чертеж

                    prSpecDraw.Exec(
                        elemData.Sign,
                        elemData.Name,
                        ownerCode,
                        synchObj.BOSTATECODE,
                        ompUserCode,
                        ref code);

                    // присоединенный файл
                    prFile.Exec(code, doc.FileName, ompUserCode, synchObj.FILEGROUP, null, ref doccode);

                    #endregion Сборочный чертеж

                    break;

                case (decimal)ObjTypes.Document:
                case (decimal)ObjTypes.UserDocument:

                    #region Документация

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

                    #endregion Документация

                    break;

                case (decimal)ObjTypes.Detail:

                    #region Деталь

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
                                iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName,
                                    modelData.Section, modelData.Position, modelData.Sign,
                                    modelData.Name, modelData.Qty, modelData.DocCode,
                                    modelData.FilePath, "Файл " + modelData.FilePath + " не найден!");
                            }
                            catch (DirectoryNotFoundException)
                            {
                                iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName,
                                    null, null, null, null, null, null, modelData.FilePath,
                                    "Задан некорректный путь к файлу: " + modelData.FilePath);
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
                                iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName,
                                    null, null, null, null, null, null, fragment.FilePath,
                                    "Файл " + fragment.FullFilePath + " не найден!");
                            }
                            catch (DirectoryNotFoundException)
                            {
                                iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName,
                                    null, null, null, null, null, null, fragment.FilePath,
                                    "Задан некорректный путь к файлу: " + fragment.FullFilePath);
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                        }
                    }

                    #endregion Деталь

                    break;

                case (decimal)ObjTypes.Fixture:

                    #region Оснастка

                    // создание оснастки
                    CreateFixture cf = new CreateFixture();
                    cf.Exec(
                        elemData.Sign,
                        elemData.Name,
                        ownerCode,
                        synchObj.BOSTATECODE,
                        ompUserCode,
                        fixTypeCode,
                        ref code);

                    // присоединенный файл
                    prFile.Exec(code, doc.FileName, ompUserCode, synchObj.FILEGROUP, null, ref doccode);

                    // модели для детали
                    var fix_models = structure.GetAllRowElements().Where(x => x.ParentRowElement == parentElem);

                    foreach (var model in fix_models)
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
                                iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName,
                                    modelData.Section, modelData.Position, modelData.Sign,
                                    modelData.Name, modelData.Qty, modelData.DocCode,
                                    modelData.FilePath, "Файл " + modelData.FilePath + " не найден!");
                            }
                            catch (DirectoryNotFoundException)
                            {
                                iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName,
                                    null, null, null, null, null, null, modelData.FilePath,
                                    "Задан некорректный путь к файлу: " + modelData.FilePath);
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
                                iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName,
                                    null, null, null, null, null, null, fragment.FilePath,
                                    "Файл " + fragment.FullFilePath + " не найден!");
                            }
                            catch (DirectoryNotFoundException)
                            {
                                iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName,
                                    null, null, null, null, null, null, fragment.FullFilePath,
                                    "Задан некорректный путь к файлу: " + fragment.FullFilePath);
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                        }
                    }

                    #endregion Оснастка

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

                    iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName, elemData.Section, elemData.Position,
                        elemData.Sign, elemData.Name, elemData.Qty, elemData.DocCode, elemData.FilePath, null);

                    // если обозначение или секция пустые, то переход на следующий элемент
                    if (elemData.Sign == String.Empty || elemData.Section == String.Empty) continue;

                    // синхронизация с КИС "Омега"
                    synchObj = synchRep.GetSynchObj(elemData.MainSection, elemData.DocCode, elemData.Sign);

                    // переход на следующий элемент, если позиция не синхронизирована
                    if (synchObj == null) continue;

                    switch (synchObj.KOTYPE)
                    {
                        case (decimal)ObjTypes.SpecFixture:

                            #region Спецификация оснастки

                            prSpec.Exec(
                                elemData.Sign,
                                elemData.Name,
                                ownerCode,
                                synchObj.BOSTATECODE,
                                ompUserCode,
                                fixTypeCode,
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
                                    iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName,
                                        elemData.Section, elemData.Position, elemData.Sign,
                                        elemData.Name, elemData.Qty, elemData.DocCode,
                                        elemData.FilePath, "Файл " + elemData.FilePath + " не найден!");
                                }
                                catch (Exception)
                                {
                                    throw;
                                }
                            }

                            #endregion Спецификация оснастки

                            break;

                        case (decimal)ObjTypes.SpecDraw:

                            #region Сборочный чертеж

                            prSpecDraw.Exec(
                                elemData.Sign,
                                elemData.Name,
                                ownerCode,
                                synchObj.BOSTATECODE,
                                ompUserCode,
                                ref code);

                            // присоединенный файл
                            prFile.Exec(code, doc.FileName, ompUserCode, synchObj.FILEGROUP, null, ref doccode);

                            #endregion Сборочный чертеж

                            break;

                        case (decimal)ObjTypes.Document:
                        case (decimal)ObjTypes.UserDocument:

                            #region Документация

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
                                    iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName,
                                        elemData.Section, elemData.Position, elemData.Sign,
                                        elemData.Name, elemData.Qty, elemData.DocCode,
                                        elemData.FilePath, "Файл " + elemData.FilePath + " не найден!");
                                }
                                catch (Exception)
                                {
                                    throw;
                                }
                            }

                            #endregion Документация

                            break;

                        case (decimal)ObjTypes.Detail:

                            #region Деталь

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

                                    iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName,
                                        elemData.Section, elemData.Position, elemData.Sign,
                                        elemData.Name, elemData.Qty, elemData.DocCode,
                                        elemData.FilePath, "Файл " + elemData.FilePath + " не найден!");
                                }
                                catch (DirectoryNotFoundException)
                                {
                                    doccode = 0;

                                    iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName,
                                        null, null, null, null, null, null, elemData.FilePath,
                                        "Задан некорректный путь к файлу: " + elemData.FilePath);
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
                                        iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName,
                                            elemData.Section, elemData.Position, elemData.Sign,
                                            elemData.Name, elemData.Qty, elemData.DocCode,
                                            elemData.FilePath, "Файл " + elemData.FilePath + " не найден!");
                                    }
                                    catch (DirectoryNotFoundException)
                                    {
                                        iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName,
                                            null, null, null, null, null, null, elemData.FilePath,
                                            "Задан некорректный путь к файлу: " + elemData.FilePath);
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
                                    iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName,
                                        elemData.Section, elemData.Position, elemData.Sign,
                                        elemData.Name, elemData.Qty, elemData.DocCode,
                                        elemData.FilePath, "Файл " + elemData.FilePath + " не найден!");
                                }
                                catch (DirectoryNotFoundException)
                                {
                                    iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName,
                                        null, null, null, null, null, null, elemData.FilePath,
                                        "Задан некорректный путь к файлу: " + elemData.FilePath);
                                }
                                catch (Exception)
                                {
                                    throw;
                                }
                            }

                            #endregion Деталь

                            break;

                        case (decimal)ObjTypes.Fixture:

                            #region Оснастка

                            CreateFixture cf = new CreateFixture();
                            cf.Exec(
                                elemData.Sign,
                                elemData.Name,
                                ownerCode,
                                synchObj.BOSTATECODE,
                                ompUserCode,
                                fixTypeCode,
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

                                    iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName,
                                        elemData.Section, elemData.Position, elemData.Sign,
                                        elemData.Name, elemData.Qty, elemData.DocCode,
                                        elemData.FilePath, "Файл " + elemData.FilePath + " не найден!");
                                }
                                catch (DirectoryNotFoundException)
                                {
                                    doccode = 0;

                                    iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName,
                                        null, null, null, null, null, null, elemData.FilePath,
                                        "Задан некорректный путь к файлу: " + elemData.FilePath);
                                }
                                catch (Exception)
                                {
                                    doccode = 0;
                                    throw;
                                }
                            }

                            // модели для детали
                            var fix_models = structure.GetAllRowElements().Where(x => x.ParentRowElement == elem);

                            foreach (var model in fix_models)
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
                                        iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName,
                                            elemData.Section, elemData.Position, elemData.Sign,
                                            elemData.Name, elemData.Qty, elemData.DocCode,
                                            elemData.FilePath, "Файл " + elemData.FilePath + " не найден!");
                                    }
                                    catch (DirectoryNotFoundException)
                                    {
                                        iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName,
                                            null, null, null, null, null, null, elemData.FilePath,
                                            "Задан некорректный путь к файлу: " + elemData.FilePath);
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
                                    iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName,
                                        elemData.Section, elemData.Position, elemData.Sign,
                                        elemData.Name, elemData.Qty, elemData.DocCode,
                                        elemData.FilePath, "Файл " + elemData.FilePath + " не найден!");
                                }
                                catch (DirectoryNotFoundException)
                                {
                                    iLog.Write(sw.Elapsed, settings.UserName, this.doc.FileName,
                                        null, null, null, null, null, null, elemData.FilePath,
                                        "Задан некорректный путь к файлу: " + elemData.FilePath);
                                }
                                catch (Exception)
                                {
                                    throw;
                                }
                            }

                            #endregion Оснастка

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

        public FixtureOmpLoad(Settings settings, ILogging iLog, Document doc, int fixType)
        {
            this.settings = settings;
            this.iLog = iLog;
            this.doc = doc;
            this.fixTypeCode = fixType;
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
                sw = new Stopwatch();
                sw.Start();

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
            finally
            {
                sw.Stop();
            }
        }
    }
}