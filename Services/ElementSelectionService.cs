using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;
using System.Linq;

namespace KRGPMagic.Plugins.FrameConnector.Services
{
    // Сервис для выбора и фильтрации элементов в Revit
    public class ElementSelectionService
    {
        #region Fields

        private readonly Document _document;
        private const string FRAME_FAMILY_NAME = "KRGP_Каркас колонны";
        private const string CONNECTOR_FAMILY_PATTERN = "KRGP_СБ";

        #endregion

        #region Constructor

        public ElementSelectionService(Document document)
        {
            _document = document;
        }

        #endregion

        #region Public Methods

        // Выбирает каркасы колонн рамкой выделения
        public List<FamilyInstance> SelectFramesByRectangle(UIDocument uiDoc)
        {
            try
            {
                var selection = uiDoc.Selection.PickElementsByRectangle("Выберите каркасы колонн рамкой");
                return FilterFrameElements(selection);
            }
            catch
            {
                return new List<FamilyInstance>();
            }
        }

        // Выбирает один каркас колонны
        public FamilyInstance SelectSingleFrame(UIDocument uiDoc)
        {
            try
            {
                var reference = uiDoc.Selection.PickObject(ObjectType.Element,
                    new FrameSelectionFilter(),
                    "Выберите каркас колонны");

                return _document.GetElement(reference) as FamilyInstance;
            }
            catch
            {
                return null;
            }
        }

        // Выбирает один соединитель
        public FamilyInstance SelectSingleConnector(UIDocument uiDoc)
        {
            try
            {
                var reference = uiDoc.Selection.PickObject(ObjectType.Element,
                    new ConnectorSelectionFilter(),
                    "Выберите соединитель");

                return _document.GetElement(reference) as FamilyInstance;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Private Methods

        // Фильтрует выбранные элементы, оставляя только каркасы колонн
        private List<FamilyInstance> FilterFrameElements(IList<Element> elements)
        {
            return elements
                .OfType<FamilyInstance>()
                .Where(fi => fi.Symbol.FamilyName == FRAME_FAMILY_NAME)
                .ToList();
        }

        #endregion

        #region Selection Filters

        // Фильтр для выбора каркасов колонн
        private class FrameSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                if (elem is FamilyInstance fi)
                {
                    return fi.Symbol.FamilyName == FRAME_FAMILY_NAME;
                }
                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }

        // Фильтр для выбора соединителей
        private class ConnectorSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                if (elem is FamilyInstance fi)
                {
                    return fi.Symbol.FamilyName.Contains(CONNECTOR_FAMILY_PATTERN);
                }
                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }

        #endregion
    }
}
