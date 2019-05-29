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
        Document = 0,
        Specification = 1,
        Detail = 2,
        SpecDraw = 6,
        UserDocument = 20,
        Complect = 22,
        SpecFixture = 31,
        Fixture = 32
    }

    public enum Sections
    {
        Documentation = 0,
        Specification = 1,
        Detail = 2,
        Complect = 22
    }

    public sealed class FixtureOmpLoad
    {
        private IDocLogging log;

        private readonly Document doc;

        private Settings settings;

        private decimal ompUserCode, ownerCode, fixTypeCode;

        private List<Document> stackDocs;

        private Stopwatch sw;

        private const string fileMain = "Основной", fileAdditional = "Дополнительный";

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
            log.Span = sw.Elapsed;
            log.User = settings.UserName;
            log.Document = this.doc.FileName;
            log.Section = elemData.Section;
            log.Position = elemData.Position;
            log.Sign = elemData.Sign;
            log.Name = elemData.Name;
            log.Qty = elemData.Qty;
            log.Doccode = elemData.DocCode;
            log.FilePath = elemData.FilePath;
            log.Error = null;

            log.Write();

            // проверка обозначения у родителя
            if (elemData.Sign == String.Empty || elemData.Section == String.Empty)
                throw new DocStructurePerentPropsException();

            // команды
            TFlexObjSynchRepository synchRep = new TFlexObjSynchRepository();

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

                    CreateSpecFix.Exec(
                        elemData.Sign,
                        elemData.Name,
                        ownerCode,
                        synchObj.BOSTATECODE,
                        ompUserCode,
                        fixTypeCode,
                        ref code);

                    // очищение спецификации
                    ClearSpecification.Exec(code);

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
                            AddFile.Exec(code, file, ompUserCode, synchObj.FILEGROUP, null, fileMain, ref doccode);
                        }
                    }

                    #endregion Спецификация оснастки

                    break;

                case (decimal)ObjTypes.SpecDraw:

                    #region Сборочный чертеж

                    CreateSpecDraw.Exec(
                        elemData.Sign,
                        elemData.Name,
                        ownerCode,
                        synchObj.BOSTATECODE,
                        ompUserCode,
                        ref code);

                    // присоединенный файл
                    AddFile.Exec(code, doc.FileName, ompUserCode, synchObj.FILEGROUP, null, fileMain, ref doccode);

                    #endregion Сборочный чертеж

                    break;

                case (decimal)ObjTypes.Document:
                case (decimal)ObjTypes.UserDocument:

                    #region Документация

                    CreateDocument.Exec(
                        synchObj.BOTYPE,
                        elemData.Sign,
                        elemData.Name,
                        ownerCode,
                        synchObj.BOSTATECODE,
                        ompUserCode,
                        ref code);

                    // присоединенный файл
                    AddFile.Exec(code, doc.FileName, ompUserCode, synchObj.FILEGROUP, null, fileMain, ref doccode);

                    #endregion Документация

                    break;

                case (decimal)ObjTypes.Detail:

                    #region Деталь

                    CreateDetail.Exec(
                        elemData.Sign,
                        elemData.Name,
                        ownerCode,
                        synchObj.BOSTATECODE,
                        ompUserCode,
                        ref code);

                    // присоединенный файл
                    AddFile.Exec(code, doc.FileName, ompUserCode, synchObj.FILEGROUP, null, fileMain, ref doccode);

                    // модели для детали
                    var models = structure.GetAllRowElements().Where(x => x.ParentRowElement == parentElem);

                    foreach (var model in models)
                    {
                        ElementDataConfig modelDataConfig = new ElementDataConfig(model, scheme);
                        ElementData modelData = modelDataConfig.ConfigData();

                        if (modelData.FilePath != null)
                        {
                            log.Span = sw.Elapsed;
                            log.User = settings.UserName;
                            log.Document = this.doc.FileName;
                            log.Section = modelData.Section;
                            log.Position = modelData.Position;
                            log.Sign = modelData.Sign;
                            log.Name = modelData.Name;
                            log.Qty = modelData.Qty;
                            log.Doccode = modelData.DocCode;
                            log.FilePath = modelData.FilePath;
                            log.Error = null;

                            decimal linkdoccode = 0;

                            AddFile.TryExec(log, code, modelData.FilePath, ompUserCode,
                                synchObj.FILEGROUP, doccode, fileAdditional, ref linkdoccode);
                        }
                    }

                    // моделями также являются невидимые фрагменты у деталей
                    foreach (var fragment in doc.GetFragments().Where(x => !x.Visible))
                    {
                        if (fragment.FullFilePath != null)
                        {
                            log.Span = sw.Elapsed;
                            log.User = settings.UserName;
                            log.Document = this.doc.FileName;
                            log.Section = null;
                            log.Position = null;
                            log.Sign = null;
                            log.Name = null;
                            log.Qty = null;
                            log.Doccode = null;
                            log.FilePath = fragment.FilePath;
                            log.Error = null;

                            decimal linkdoccode = 0;

                            AddFile.TryExec(log, code, fragment.FullFilePath, ompUserCode,
                                synchObj.FILEGROUP, doccode, fileAdditional, ref linkdoccode);
                        }
                    }

                    #endregion Деталь

                    break;

                case (decimal)ObjTypes.Fixture:

                    #region Оснастка

                    CreateFixture.Exec(
                        elemData.Sign,
                        elemData.Name,
                        ownerCode,
                        synchObj.BOSTATECODE,
                        ompUserCode,
                        fixTypeCode,
                        ref code);

                    // присоединенный файл
                    AddFile.Exec(code, doc.FileName, ompUserCode, synchObj.FILEGROUP, null, fileMain, ref doccode);

                    // модели для детали
                    var fix_models = structure.GetAllRowElements().Where(x => x.ParentRowElement == parentElem);

                    foreach (var model in fix_models)
                    {
                        ElementDataConfig modelDataConfig = new ElementDataConfig(model, scheme);
                        ElementData modelData = modelDataConfig.ConfigData();

                        if (modelData.FilePath != null)
                        {
                            log.Span = sw.Elapsed;
                            log.User = settings.UserName;
                            log.Document = this.doc.FileName;
                            log.Section = modelData.Section;
                            log.Position = modelData.Position;
                            log.Sign = modelData.Sign;
                            log.Name = modelData.Name;
                            log.Qty = modelData.Qty;
                            log.Doccode = modelData.DocCode;
                            log.FilePath = modelData.FilePath;
                            log.Error = null;

                            decimal linkdoccode = 0;

                            AddFile.TryExec(log, code, modelData.FilePath, ompUserCode,
                                synchObj.FILEGROUP, doccode, fileAdditional, ref linkdoccode);
                        }
                    }

                    // моделями также являются невидимые фрагменты у деталей
                    foreach (var fragment in doc.GetFragments().Where(x => !x.Visible))
                    {
                        if (fragment.FullFilePath != null)
                        {
                            log.Span = sw.Elapsed;
                            log.User = settings.UserName;
                            log.Document = this.doc.FileName;
                            log.Section = null;
                            log.Position = null;
                            log.Sign = null;
                            log.Name = null;
                            log.Qty = null;
                            log.Doccode = null;
                            log.FilePath = fragment.FilePath;
                            log.Error = null;

                            decimal linkdoccode = 0;

                            AddFile.TryExec(log, code, fragment.FullFilePath, ompUserCode,
                                synchObj.FILEGROUP, doccode, fileAdditional, ref linkdoccode);
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

                    // логирование
                    log.Span = sw.Elapsed;
                    log.User = settings.UserName;
                    log.Document = this.doc.FileName;
                    log.Section = elemData.Section;
                    log.Position = elemData.Position;
                    log.Sign = elemData.Sign;
                    log.Name = elemData.Name;
                    log.Qty = elemData.Qty;
                    log.Doccode = elemData.DocCode;
                    log.FilePath = elemData.FilePath;
                    log.Error = null;

                    log.Write();

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

                            CreateSpecFix.Exec(
                                elemData.Sign,
                                elemData.Name,
                                ownerCode,
                                synchObj.BOSTATECODE,
                                ompUserCode,
                                fixTypeCode,
                                ref code);

                            // очищение спецификации
                            ClearSpecification.Exec(code);

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
                                catch (FileNotFoundException e)
                                {
                                    log.Span = sw.Elapsed;
                                    log.User = settings.UserName;
                                    log.Document = this.doc.FileName;
                                    log.Section = elemData.Section;
                                    log.Position = elemData.Position;
                                    log.Sign = elemData.Sign;
                                    log.Name = elemData.Name;
                                    log.Qty = elemData.Qty;
                                    log.Doccode = elemData.DocCode;
                                    log.FilePath = elemData.FilePath;
                                    log.Error = null;

                                    log.Write(e);
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

                            CreateSpecDraw.Exec(
                                elemData.Sign,
                                elemData.Name,
                                ownerCode,
                                synchObj.BOSTATECODE,
                                ompUserCode,
                                ref code);

                            // присоединенный файл
                            if (elemData.FilePath != null)
                            {
                                AddFile.Exec(code, doc.FileName, ompUserCode, synchObj.FILEGROUP, null, fileMain, ref doccode);
                            }

                            #endregion Сборочный чертеж

                            break;

                        case (decimal)ObjTypes.Document:
                        case (decimal)ObjTypes.UserDocument:

                            #region Документация

                            CreateDocument.Exec(
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
                                AddFile.Exec(code, elemData.FilePath, ompUserCode, synchObj.FILEGROUP, null, fileMain, ref doccode);
                            }

                            #endregion Документация

                            break;

                        case (decimal)ObjTypes.Detail:

                            #region Деталь

                            CreateDetail.Exec(
                                elemData.Sign,
                                elemData.Name,
                                ownerCode,
                                synchObj.BOSTATECODE,
                                ompUserCode,
                                ref code);

                            // файл
                            if (elemData.FilePath != null)
                            {
                                AddFile.Exec(code, elemData.FilePath, ompUserCode, synchObj.FILEGROUP, null, fileMain, ref doccode);
                            }

                            // модели для детали
                            var models = structure.GetAllRowElements().Where(x => x.ParentRowElement == elem);

                            foreach (var model in models)
                            {
                                ElementDataConfig modelDataConfig = new ElementDataConfig(model, scheme);
                                ElementData modelData = modelDataConfig.ConfigData();

                                if (modelData.FilePath != null)
                                {
                                    log.Span = sw.Elapsed;
                                    log.User = settings.UserName;
                                    log.Document = this.doc.FileName;
                                    log.Section = modelData.Section;
                                    log.Position = modelData.Position;
                                    log.Sign = modelData.Sign;
                                    log.Name = modelData.Name;
                                    log.Qty = modelData.Qty;
                                    log.Doccode = modelData.DocCode;
                                    log.FilePath = modelData.FilePath;
                                    log.Error = null;

                                    decimal linkdoccode = 0;

                                    AddFile.TryExec(log, code, modelData.FilePath, ompUserCode,
                                        synchObj.FILEGROUP, (doccode == 0) ? null : (decimal?)doccode, fileAdditional, ref linkdoccode);
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
                                catch (FileNotFoundException e)
                                {
                                    log.Span = sw.Elapsed;
                                    log.User = settings.UserName;
                                    log.Document = this.doc.FileName;
                                    log.Section = elemData.Section;
                                    log.Position = elemData.Position;
                                    log.Sign = elemData.Sign;
                                    log.Name = elemData.Name;
                                    log.Qty = elemData.Qty;
                                    log.Doccode = elemData.DocCode;
                                    log.FilePath = elemData.FilePath;
                                    log.Error = null;

                                    log.Write(e);
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

                            CreateFixture.Exec(
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
                                AddFile.Exec(code, elemData.FilePath, ompUserCode, synchObj.FILEGROUP, null, fileMain, ref doccode);
                            }

                            // модели для детали
                            var fix_models = structure.GetAllRowElements().Where(x => x.ParentRowElement == elem);

                            foreach (var model in fix_models)
                            {
                                ElementDataConfig modelDataConfig = new ElementDataConfig(model, scheme);
                                ElementData modelData = modelDataConfig.ConfigData();

                                if (modelData.FilePath != null)
                                {
                                    log.Span = sw.Elapsed;
                                    log.User = settings.UserName;
                                    log.Document = this.doc.FileName;
                                    log.Section = modelData.Section;
                                    log.Position = modelData.Position;
                                    log.Sign = modelData.Sign;
                                    log.Name = modelData.Name;
                                    log.Qty = modelData.Qty;
                                    log.Doccode = modelData.DocCode;
                                    log.FilePath = modelData.FilePath;
                                    log.Error = null;

                                    decimal linkdoccode = 0;

                                    AddFile.Exec(code, modelData.FilePath, ompUserCode, synchObj.FILEGROUP,
                                        (doccode == 0) ? null : (decimal?)doccode, fileAdditional, ref linkdoccode);
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
                                catch (FileNotFoundException e)
                                {
                                    log.Span = sw.Elapsed;
                                    log.User = settings.UserName;
                                    log.Document = this.doc.FileName;
                                    log.Section = elemData.Section;
                                    log.Position = elemData.Position;
                                    log.Sign = elemData.Sign;
                                    log.Name = elemData.Name;
                                    log.Qty = elemData.Qty;
                                    log.Doccode = elemData.DocCode;
                                    log.FilePath = elemData.FilePath;
                                    log.Error = null;

                                    log.Write(e);
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
                        AddElement.Exec(
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

        public FixtureOmpLoad(Settings settings, IDocLogging iLog, Document doc, int fixType)
        {
            this.settings = settings;
            this.log = iLog;
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