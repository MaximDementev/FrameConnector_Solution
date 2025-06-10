using Autodesk.Revit.DB;
using KRGPMagic.Core.Services;
using KRGPMagic.Plugins.FrameConnector.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace KRGPMagic.Plugins.FrameConnector.UI
{
    // Форма для группировки каркасов и выбора соединителей
    public partial class FrameGroupingForm : System.Windows.Forms.Form
    {
        #region Fields

        private readonly List<FamilyInstance> _frames;
        private readonly Document _document;
        private readonly ParameterMappingService _parameterService;
        private readonly SettingsService _settingsService;
        private readonly ConnectorTypeService _connectorTypeService;

        // Элементы управления формы - должны быть объявлены здесь для дизайнера
        private Label _titleLabel;
        private Label _parameterLabel;
        private ComboBox _groupingParameterComboBox;
        private Button _groupButton;
        private DataGridView _groupsDataGridView;
        private Button _createConnectorsButton;
        private Button _cancelButton;
        private System.ComponentModel.IContainer components = null; // Для Dispose

        private const string CONNECTOR_TYPE_COLUMN_NAME = "ConnectorTypeColumn";
        private const string GROUP_KEY_COLUMN_NAME = "GroupKeyColumn";
        private const string NO_CONNECTOR_OPTION = "-не устанавливать соединители-";

        #endregion

        #region Properties

        public Dictionary<string, List<FamilyInstance>> GroupedFrames { get; private set; }
        public Dictionary<string, FamilySymbol> ConnectorMappings { get; private set; }

        #endregion

        #region Constructor

        public FrameGroupingForm(List<FamilyInstance> frames, Document document, ConnectorTypeService connectorTypeService, IPathService pathService)
        {
            _frames = frames ?? throw new ArgumentNullException(nameof(frames));
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _connectorTypeService = connectorTypeService ?? throw new ArgumentNullException(nameof(connectorTypeService));

            _parameterService = new ParameterMappingService(document);
            _settingsService = new SettingsService(pathService);

            GroupedFrames = new Dictionary<string, List<FamilyInstance>>();
            ConnectorMappings = new Dictionary<string, FamilySymbol>();

            InitializeComponent(); // Стандартный метод инициализации WinForms

            // Установка текста, зависящего от данных, ПОСЛЕ InitializeComponent
            _titleLabel.Text = $"Выбрано каркасов: {_frames.Count}";

            LoadAvailableParameters();
            LoadSettings();
        }

        #endregion

        #region Dispose

        // Очистка ресурсов формы
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion

        #region Form Initialization

        // Инициализирует компоненты формы (генерируется дизайнером или пишется вручную)
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container(); // Для Dispose
            this._titleLabel = new System.Windows.Forms.Label();
            this._parameterLabel = new System.Windows.Forms.Label();
            this._groupingParameterComboBox = new System.Windows.Forms.ComboBox();
            this._groupButton = new System.Windows.Forms.Button();
            this._groupsDataGridView = new System.Windows.Forms.DataGridView();
            this._createConnectorsButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this._groupsDataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // _titleLabel
            // 
            this._titleLabel.Location = new System.Drawing.Point(12, 12);
            this._titleLabel.Name = "_titleLabel";
            this._titleLabel.Size = new System.Drawing.Size(300, 23);
            this._titleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            // Текст будет установлен в конструкторе после InitializeComponent
            // 
            // _parameterLabel
            // 
            this._parameterLabel.Location = new System.Drawing.Point(12, 45);
            this._parameterLabel.Name = "_parameterLabel";
            this._parameterLabel.Size = new System.Drawing.Size(150, 23);
            this._parameterLabel.Text = "Параметр для группировки:";
            // 
            // _groupingParameterComboBox
            // 
            this._groupingParameterComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._groupingParameterComboBox.FormattingEnabled = true;
            this._groupingParameterComboBox.Location = new System.Drawing.Point(170, 42);
            this._groupingParameterComboBox.Name = "_groupingParameterComboBox";
            this._groupingParameterComboBox.Size = new System.Drawing.Size(300, 21); // Стандартная высота для ComboBox
            // 
            // _groupButton
            // 
            this._groupButton.Location = new System.Drawing.Point(480, 41);
            this._groupButton.Name = "_groupButton";
            this._groupButton.Size = new System.Drawing.Size(100, 25);
            this._groupButton.Text = "Сгруппировать";
            this._groupButton.UseVisualStyleBackColor = true;
            this._groupButton.Click += new System.EventHandler(this.GroupButton_Click);
            // 
            // _groupsDataGridView
            // 
            this._groupsDataGridView.AllowUserToAddRows = false;
            this._groupsDataGridView.AllowUserToDeleteRows = false;
            this._groupsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this._groupsDataGridView.Location = new System.Drawing.Point(12, 80);
            this._groupsDataGridView.Name = "_groupsDataGridView";
            this._groupsDataGridView.RowHeadersVisible = false;
            this._groupsDataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._groupsDataGridView.Size = new System.Drawing.Size(560, 350);
            // 
            // _createConnectorsButton
            // 
            this._createConnectorsButton.Enabled = false;
            this._createConnectorsButton.Location = new System.Drawing.Point(380, 440);
            this._createConnectorsButton.Name = "_createConnectorsButton";
            this._createConnectorsButton.Size = new System.Drawing.Size(130, 30);
            this._createConnectorsButton.Text = "Создать соединители";
            this._createConnectorsButton.UseVisualStyleBackColor = true;
            this._createConnectorsButton.Click += new System.EventHandler(this.CreateConnectorsButton_Click);
            // 
            // _cancelButton
            // 
            this._cancelButton.Location = new System.Drawing.Point(520, 440);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(75, 30);
            this._cancelButton.Text = "Отмена";
            this._cancelButton.UseVisualStyleBackColor = true;
            this._cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // FrameGroupingForm
            // 
            this.ClientSize = new System.Drawing.Size(594, 481); // Стандартные размеры могут немного отличаться
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._createConnectorsButton);
            this.Controls.Add(this._groupsDataGridView);
            this.Controls.Add(this._groupButton);
            this.Controls.Add(this._groupingParameterComboBox);
            this.Controls.Add(this._parameterLabel);
            this.Controls.Add(this._titleLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrameGroupingForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Группировка каркасов и выбор соединителей";
            ((System.ComponentModel.ISupportInitialize)(this._groupsDataGridView)).EndInit();
            this.ResumeLayout(false);

            // Инициализация колонок DataGridView ПОСЛЕ создания самого DataGridView
            InitializeDataGridViewColumns();
        }

        // Инициализирует колонки для DataGridView
        private void InitializeDataGridViewColumns()
        {
            // Очищаем существующие колонки (если есть)
            _groupsDataGridView.Columns.Clear();

            // Колонка для ключа группы (текстовая)
            var groupKeyColumn = new DataGridViewTextBoxColumn
            {
                Name = GROUP_KEY_COLUMN_NAME,
                HeaderText = "Группа (значение параметра)",
                ReadOnly = true,
                FillWeight = 40 // Пропорциональная ширина
            };
            _groupsDataGridView.Columns.Add(groupKeyColumn);

            // Колонка для выбора типа соединителя (ComboBox)
            var connectorTypeColumn = new DataGridViewComboBoxColumn
            {
                Name = CONNECTOR_TYPE_COLUMN_NAME,
                HeaderText = "Тип соединителя",
                FlatStyle = FlatStyle.Flat, // Для лучшего вида в DataGridView
                FillWeight = 60 // Пропорциональная ширина
            };
            _groupsDataGridView.Columns.Add(connectorTypeColumn);
        }

        #endregion

        #region Event Handlers

        // Обработчик нажатия кнопки группировки
        private void GroupButton_Click(object sender, EventArgs e)
        {
            try
            {
                var selectedParameter = _groupingParameterComboBox.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(selectedParameter))
                {
                    MessageBox.Show("Выберите параметр для группировки.", "Предупреждение",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                GroupFramesByParameter(selectedParameter);
                PopulateDataGridView();

                _createConnectorsButton.Enabled = GroupedFrames.Any();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при группировке: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Обработчик нажатия кнопки создания соединителей
        private void CreateConnectorsButton_Click(object sender, EventArgs e)
        {
            try
            {
                CollectConnectorMappingsFromDataGridView();
                SaveSettings();

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подготовке данных: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Обработчик нажатия кнопки отмены
        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        #endregion

        #region Data Population and Retrieval

        // Загружает доступные параметры для группировки в ComboBox
        private void LoadAvailableParameters()
        {
            var allParameters = new HashSet<string>();

            foreach (var frame in _frames)
            {
                var parameters = _parameterService.GetElementParameters(frame);
                foreach (var param in parameters)
                {
                    allParameters.Add(param);
                }
            }

            _groupingParameterComboBox.Items.Clear(); // Очищаем перед добавлением
            _groupingParameterComboBox.Items.AddRange(allParameters.OrderBy(p => p).ToArray());
        }

        // Группирует каркасы по выбранному параметру
        private void GroupFramesByParameter(string parameterName)
        {
            GroupedFrames.Clear();

            foreach (var frame in _frames)
            {
                var parameter = frame.LookupParameter(parameterName) ??
                               _document.GetElement(frame.GetTypeId())?.LookupParameter(parameterName);

                string groupKey = "Без значения";
                if (parameter != null && parameter.HasValue)
                {
                    groupKey = parameter.AsValueString() ?? parameter.AsString() ?? "Без значения";
                }

                if (!GroupedFrames.ContainsKey(groupKey))
                {
                    GroupedFrames[groupKey] = new List<FamilyInstance>();
                }

                GroupedFrames[groupKey].Add(frame);
            }
        }

        // Заполняет DataGridView данными о группах и доступными соединителями
        private void PopulateDataGridView()
        {
            // Проверяем, что колонки уже созданы
            if (_groupsDataGridView.Columns.Count == 0)
            {
                InitializeDataGridViewColumns();
            }

            _groupsDataGridView.Rows.Clear();

            var connectorTypesDict = _connectorTypeService.GetConnectorTypesWithNames();
            var connectorDisplayItems = new List<string> { NO_CONNECTOR_OPTION };
            connectorDisplayItems.AddRange(connectorTypesDict.Keys.OrderBy(k => k));

            // Обновляем элементы в ComboBox колонке
            var comboBoxColumn = _groupsDataGridView.Columns[CONNECTOR_TYPE_COLUMN_NAME] as DataGridViewComboBoxColumn;
            if (comboBoxColumn != null)
            {
                comboBoxColumn.Items.Clear();
                comboBoxColumn.Items.AddRange(connectorDisplayItems.ToArray());
            }

            var savedSettings = _settingsService.LoadSettings();

            foreach (var group in GroupedFrames.OrderBy(g => g.Key))
            {
                int rowIndex = _groupsDataGridView.Rows.Add();
                DataGridViewRow row = _groupsDataGridView.Rows[rowIndex];

                // Устанавливаем значение для текстовой колонки
                row.Cells[GROUP_KEY_COLUMN_NAME].Value = $"{group.Key} ({group.Value.Count} шт.)";

                // Устанавливаем значение для ComboBox колонки
                var cellComboBox = row.Cells[CONNECTOR_TYPE_COLUMN_NAME] as DataGridViewComboBoxCell;
                if (cellComboBox != null)
                {
                    string defaultValue = NO_CONNECTOR_OPTION;
                    if (savedSettings.ConnectorTypeMappings.ContainsKey(group.Key))
                    {
                        var savedMapping = savedSettings.ConnectorTypeMappings[group.Key];
                        if (connectorDisplayItems.Contains(savedMapping))
                        {
                            defaultValue = savedMapping;
                        }
                    }
                    cellComboBox.Value = defaultValue;
                }
            }
        }

        // Собирает сопоставления соединителей из DataGridView
        private void CollectConnectorMappingsFromDataGridView()
        {
            ConnectorMappings.Clear();
            var connectorTypesDict = _connectorTypeService.GetConnectorTypesWithNames();

            foreach (DataGridViewRow row in _groupsDataGridView.Rows)
            {
                if (row.IsNewRow) continue;

                string groupCellText = row.Cells[GROUP_KEY_COLUMN_NAME].Value?.ToString() ?? string.Empty;
                string groupKey = ExtractGroupKeyFromCellText(groupCellText);

                var selectedItem = row.Cells[CONNECTOR_TYPE_COLUMN_NAME].Value?.ToString();

                if (!string.IsNullOrEmpty(selectedItem) && selectedItem != NO_CONNECTOR_OPTION)
                {
                    if (connectorTypesDict.ContainsKey(selectedItem))
                    {
                        ConnectorMappings[groupKey] = connectorTypesDict[selectedItem];
                    }
                }
                else
                {
                    ConnectorMappings[groupKey] = null; // Явно указываем null, если не выбран соединитель
                }
            }
        }

        // Извлекает ключ группы из текста ячейки (например, "Значение (5 шт.)" -> "Значение")
        private string ExtractGroupKeyFromCellText(string cellText)
        {
            if (string.IsNullOrEmpty(cellText)) return string.Empty;
            int lastParenthesis = cellText.LastIndexOf(" (");
            if (lastParenthesis > 0)
            {
                return cellText.Substring(0, lastParenthesis).Trim();
            }
            return cellText.Trim();
        }

        #endregion

        #region Settings Management

        // Загружает сохраненные настройки
        private void LoadSettings()
        {
            try
            {
                var settings = _settingsService.LoadSettings();

                if (!string.IsNullOrEmpty(settings.LastGroupingParameter) &&
                    _groupingParameterComboBox.Items.Cast<string>().Contains(settings.LastGroupingParameter))
                {
                    _groupingParameterComboBox.SelectedItem = settings.LastGroupingParameter;
                }
                // Загрузка сопоставлений для DataGridView происходит в PopulateDataGridView при его заполнении
            }
            catch
            {
                // Игнорируем ошибки загрузки настроек
            }
        }

        // Сохраняет текущие настройки
        private void SaveSettings()
        {
            try
            {
                var selectedParameter = _groupingParameterComboBox.SelectedItem?.ToString();
                var typeMappings = new Dictionary<string, string>();

                foreach (DataGridViewRow row in _groupsDataGridView.Rows)
                {
                    if (row.IsNewRow) continue;

                    string groupCellText = row.Cells[GROUP_KEY_COLUMN_NAME].Value?.ToString() ?? string.Empty;
                    string groupKey = ExtractGroupKeyFromCellText(groupCellText);

                    var selectedItem = row.Cells[CONNECTOR_TYPE_COLUMN_NAME].Value?.ToString();
                    if (!string.IsNullOrEmpty(selectedItem)) // Сохраняем даже если это NO_CONNECTOR_OPTION
                    {
                        typeMappings[groupKey] = selectedItem;
                    }
                }

                _settingsService.SaveGroupingSettings(selectedParameter, typeMappings);
            }
            catch
            {
                // Игнорируем ошибки сохранения настроек
            }
        }

        #endregion
    }
}
