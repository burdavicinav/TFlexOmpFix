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
        private readonly IDocLogging log;

        private readonly Document doc;

        private readonly Settings settings;

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

        private void IsValidStructure(ProductStructure structure)
        {
            Document currentDoc = structure.Document;

            // существование родителя
            if (structure.GetAllRowElements().Where(x => x.ParentRowElement == null).Count() != 1)
            {
                throw new DocStructureParentException(currentDoc.FileName);
            }

            // проверка обозначения родителя на валидность
            RowElement parentItem = structure.GetAllRowElements().Where(
                x => x.ParentRowElement == null).FirstOrDefault();

            SchemeDataConfig schemeConfig = new SchemeDataConfig(structure);
            SchemeData scheme = schemeConfig.GetScheme();

            ElementDataConfig dataConfig = new ElementDataConfig(parentItem, scheme);

            ElementData itemData = dataConfig.ConfigData();

            if (itemData.Sign == string.Empty ||
                 itemData.MainSection != "Документация" && itemData.Sign.Contains("СБ"))
            {
                throw new DocStructureParentSignNotValidException(currentDoc.FileName);
            }

            // проверка типа документа у родителя
            if ((itemData.MainSection == "Документация" ||
                itemData.MainSection == "Сборочные единицы" ||
                itemData.MainSection == "Комплекты")
                && itemData.DocCode == string.Empty)
            {
                throw new DocStructureDocTypeNotValidException(currentDoc.FileName);
            }

            // проверка обозначения у родителя
            if (itemData.Sign == String.Empty || itemData.Section == String.Empty)
            {
                throw new DocStructurePerentPropsException();
            }
        }

        private void ExportStructure(ProductStructure structure, bool isConfig = false)
        {
            // верификация структуры
            IsValidStructure(structure);

            // текущий документ
            Document currentDoc = structure.Document;

            // схема структуры
            SchemeDataConfig schemeConfig = new SchemeDataConfig(structure);
            SchemeData scheme = schemeConfig.GetScheme();

            // родительский элемент
            RowElement parentItem = structure.GetAllRowElements().Where(
                x => x.ParentRowElement == null).FirstOrDefault();

            ElementDataConfig dataConfig = new ElementDataConfig(parentItem, scheme);
            ElementData itemData = dataConfig.ConfigData();

            // если структура является исполнением, то обозначением головного элемента
            // является наименование структуры
            if (isConfig)
            {
                itemData.Sign = structure.Name;
            }

            // логирование
            log.Span = sw.Elapsed;
            log.User = settings.UserName;
            log.Document = this.doc.FileName;
            log.Section = itemData.Section;
            log.Position = itemData.Position;
            log.Sign = itemData.Sign;
            log.Name = itemData.Name;
            log.Qty = itemData.Qty;
            log.Doccode = itemData.DocCode;
            log.FilePath = itemData.FilePath;
            log.Error = null;

            log.Write();

            // команды
            TFlexObjSynchRepository synchRep = new TFlexObjSynchRepository();

            // родительский элемент
            decimal? parent = null;

            // код БД
            decimal code = 0;
            decimal doccode = 0;

            // синхронизация с КИС "Омега"
            V_SEPO_TFLEX_OBJ_SYNCH synchObj = synchRep.GetSynchObj(
                itemData.MainSection,
                itemData.DocCode,
                itemData.Sign,
                itemData.ObjectType);

            // если головной элемент не синхронизирован - выход
            if (synchObj == null) return;

            #region экспорт родителя

            switch (synchObj.KOTYPE)
            {
                case (decimal)ObjTypes.SpecFixture:

                    #region Спецификация оснастки

                    CreateSpecFix.Exec(
                        itemData.Sign,
                        itemData.Name,
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
                    string signPattern = Regex.Replace(itemData.Sign, @"\D", "");

                    string[] files = Directory.GetFiles(currentDoc.FilePath);

                    foreach (var file in files)
                    {
                        if (file.Contains(itemData.Sign) && file.Contains("СП"))
                        {
                            AddFile.Exec(code, file, ompUserCode, synchObj.FILEGROUP, null, fileMain, ref doccode);
                        }
                    }

                    #endregion Спецификация оснастки

                    break;

                case (decimal)ObjTypes.SpecDraw:

                    #region Сборочный чертеж

                    CreateSpecDraw.Exec(
                        itemData.Sign,
                        itemData.Name,
                        ownerCode,
                        synchObj.BOSTATECODE,
                        ompUserCode,
                        ref code);

                    // присоединенный файл
                    AddFile.Exec(code, currentDoc.FileName, ompUserCode, synchObj.FILEGROUP, null, fileMain, ref doccode);

                    #endregion Сборочный чертеж

                    break;

                case (decimal)ObjTypes.Document:
                case (decimal)ObjTypes.UserDocument:

                    #region Документация

                    CreateDocument.Exec(
                        synchObj.BOTYPE,
                        itemData.Sign,
                        itemData.Name,
                        ownerCode,
                        synchObj.BOSTATECODE,
                        ompUserCode,
                        ref code);

                    // присоединенный файл
                    AddFile.Exec(code, currentDoc.FileName, ompUserCode, synchObj.FILEGROUP, null, fileMain, ref doccode);

                    #endregion Документация

                    break;

                case (decimal)ObjTypes.Detail:

                    #region Деталь

                    CreateDetail.Exec(
                        itemData.Sign,
                        itemData.Name,
                        ownerCode,
                        synchObj.BOSTATECODE,
                        ompUserCode,
                        ref code);

                    // присоединенный файл
                    AddFile.Exec(code, currentDoc.FileName, ompUserCode, synchObj.FILEGROUP, null, fileMain, ref doccode);

                    // модели для детали
                    var models = structure.GetAllRowElements().Where(x => x.ParentRowElement == parentItem);

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
                    foreach (var fragment in currentDoc.GetFragments().Where(x => !x.Visible))
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
                        itemData.Sign,
                        itemData.Name,
                        ownerCode,
                        synchObj.BOSTATECODE,
                        ompUserCode,
                        fixTypeCode,
                        ref code);

                    // присоединенный файл
                    AddFile.Exec(code, currentDoc.FileName, ompUserCode, synchObj.FILEGROUP, null, fileMain, ref doccode);

                    // модели для детали
                    var fix_models = structure.GetAllRowElements().Where(x => x.ParentRowElement == parentItem);

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
                    foreach (var fragment in currentDoc.GetFragments().Where(x => !x.Visible))
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

            if (itemData.MainSection == "Сборочные единицы")
            {
                foreach (var elem in
                    structure
                    .GetAllRowElements()
                    .Where(x => x.ParentRowElement == parentItem)
                    )
                {
                    // получение данных о документе
                    dataConfig = new ElementDataConfig(elem, scheme);

                    itemData = dataConfig.ConfigData();

                    // логирование
                    log.Span = sw.Elapsed;
                    log.User = settings.UserName;
                    log.Document = this.doc.FileName;
                    log.Section = itemData.Section;
                    log.Position = itemData.Position;
                    log.Sign = itemData.Sign;
                    log.Name = itemData.Name;
                    log.Qty = itemData.Qty;
                    log.Doccode = itemData.DocCode;
                    log.FilePath = itemData.FilePath;
                    log.Error = null;

                    log.Write();

                    // если обозначение или секция пустые, то переход на следующий элемент
                    if (itemData.Sign == String.Empty || itemData.Section == String.Empty) continue;

                    //System.Windows.Forms.MessageBox.Show(itemData.SignFull + " |" + itemData.Section + " | "
                    //    + itemData.MainSection + " | " + itemData.ObjectType);

                    // синхронизация с КИС "Омега"
                    synchObj = synchRep.GetSynchObj(
                        itemData.MainSection,
                        itemData.DocCode,
                        itemData.Sign,
                        itemData.ObjectType);

                    // переход на следующий элемент, если позиция не синхронизирована
                    if (synchObj == null) continue;

                    switch (synchObj.KOTYPE)
                    {
                        case (decimal)ObjTypes.SpecFixture:

                            #region Спецификация оснастки

                            CreateSpecFix.Exec(
                                itemData.SignFull,
                                itemData.Name,
                                ownerCode,
                                synchObj.BOSTATECODE,
                                ompUserCode,
                                fixTypeCode,
                                ref code);

                            // очищение спецификации
                            ClearSpecification.Exec(code);

                            // если у сборки есть фрагмент...
                            if (itemData.FilePath != null)
                            {
                                try
                                {
                                    if (!File.Exists(itemData.FilePath)) throw new FileNotFoundException();

                                    // открыть документ входящей сборки в режиме чтения
                                    Document linkDoc = TFlex.Application.OpenDocument(itemData.FilePath, false, true);

                                    // добавить документ в стек
                                    stackDocs.Add(linkDoc);

                                    // экспорт документа
                                    ExportDoc(linkDoc, itemData.SignFull);

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
                                    log.Section = itemData.Section;
                                    log.Position = itemData.Position;
                                    log.Sign = itemData.Sign;
                                    log.Name = itemData.Name;
                                    log.Qty = itemData.Qty;
                                    log.Doccode = itemData.DocCode;
                                    log.FilePath = itemData.FilePath;
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
                                itemData.Sign,
                                itemData.Name,
                                ownerCode,
                                synchObj.BOSTATECODE,
                                ompUserCode,
                                ref code);

                            // присоединенный файл
                            AddFile.Exec(code, currentDoc.FileName, ompUserCode, synchObj.FILEGROUP, null, fileMain, ref doccode);

                            #endregion Сборочный чертеж

                            break;

                        case (decimal)ObjTypes.Document:
                        case (decimal)ObjTypes.UserDocument:

                            #region Документация

                            CreateDocument.Exec(
                                synchObj.BOTYPE,
                                itemData.Sign,
                                itemData.Name,
                                ownerCode,
                                synchObj.BOSTATECODE,
                                ompUserCode,
                                ref code);

                            // файл
                            if (itemData.FilePath != null)
                            {
                                AddFile.Exec(code, itemData.FilePath, ompUserCode, synchObj.FILEGROUP, null, fileMain, ref doccode);
                            }

                            #endregion Документация

                            break;

                        case (decimal)ObjTypes.Detail:

                            #region Деталь

                            CreateDetail.Exec(
                                itemData.SignFull,
                                itemData.Name,
                                ownerCode,
                                synchObj.BOSTATECODE,
                                ompUserCode,
                                ref code);

                            // файл
                            if (itemData.FilePath != null)
                            {
                                AddFile.Exec(code, itemData.FilePath, ompUserCode, synchObj.FILEGROUP, null, fileMain, ref doccode);
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
                            if (itemData.FilePath != null)
                            {
                                try
                                {
                                    if (!File.Exists(itemData.FilePath)) throw new FileNotFoundException();

                                    // открыть документ входящей сборки в режиме чтения
                                    Document linkDoc = TFlex.Application.OpenDocument(itemData.FilePath, false, true);

                                    // добавить документ в стек
                                    stackDocs.Add(linkDoc);

                                    // экспорт документа
                                    ExportDoc(linkDoc, itemData.SignFull);

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
                                    log.Section = itemData.Section;
                                    log.Position = itemData.Position;
                                    log.Sign = itemData.Sign;
                                    log.Name = itemData.Name;
                                    log.Qty = itemData.Qty;
                                    log.Doccode = itemData.DocCode;
                                    log.FilePath = itemData.FilePath;
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
                                itemData.SignFull,
                                itemData.Name,
                                ownerCode,
                                synchObj.BOSTATECODE,
                                ompUserCode,
                                fixTypeCode,
                                ref code);

                            // файл
                            if (itemData.FilePath != null)
                            {
                                AddFile.Exec(code, itemData.FilePath, ompUserCode, synchObj.FILEGROUP, null, fileMain, ref doccode);
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
                            if (itemData.FilePath != null)
                            {
                                try
                                {
                                    if (!File.Exists(itemData.FilePath)) throw new FileNotFoundException();

                                    // открыть документ входящей сборки в режиме чтения
                                    Document linkDoc = TFlex.Application.OpenDocument(itemData.FilePath, false, true);

                                    // добавить документ в стек
                                    stackDocs.Add(linkDoc);

                                    // экспорт документа
                                    ExportDoc(linkDoc, itemData.SignFull);

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
                                    log.Section = itemData.Section;
                                    log.Position = itemData.Position;
                                    log.Sign = itemData.Sign;
                                    log.Name = itemData.Name;
                                    log.Qty = itemData.Qty;
                                    log.Doccode = itemData.DocCode;
                                    log.FilePath = itemData.FilePath;
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
                            itemData.Qty.Value,
                            itemData.Position,
                            ompUserCode);
                    }
                }
            }

            #endregion элементы спецификации
        }

        private void ExportDoc(Document document, string parent = null)
        {
            ProductStructure structure;

            // если в документе нет исполнений...
            if (document.ModelConfigurations.ConfigurationCount == 0)
            {
                // проверка на единственность структуры
                if (document.GetProductStructures().Count() != 1)
                {
                    throw new DocStructureException(document.FileName);
                }

                structure = document
                    .GetProductStructures()
                    .FirstOrDefault();

                ExportStructure(structure);
            }
            else
            {
                if (parent == null)
                {
                    for (int i = 0; i < document.ModelConfigurations.ConfigurationCount; i++)
                    {
                        structure = document
                            .GetProductStructures()
                            .Where(x => x.Name == document.ModelConfigurations.GetConfigurationName(i))
                            .FirstOrDefault();

                        if (structure == null)
                        {
                            throw new DocStructureException(document.FileName);
                        }

                        ExportStructure(structure, true);
                    }
                }
                else
                {
                    structure = document
                            .GetProductStructures()
                            .Where(x => x.Name == parent)
                            .FirstOrDefault();

                    if (structure == null)
                    {
                        throw new DocStructureException(document.FileName);
                    }

                    ExportStructure(structure, true);
                }
            }
        }

        public FixtureOmpLoad(Settings settings, IDocLogging iLog, Document doc, int fixType)
        {
            this.settings = settings;
            this.log = iLog;
            this.doc = doc;
            this.fixTypeCode = fixType;
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