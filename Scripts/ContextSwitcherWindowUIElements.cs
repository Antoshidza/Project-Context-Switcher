using ProjectContextSwitcher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public static class ContextSwitcherWindowUIElements
{
    private const string RootFolderPath = "Packages/com.tonymax.pcw/";
    private const string ProjectSettingAssetPath = "ProjectSettings/ProjectContextsSettings.asset";
    private const string ProjectContextWindowUI_VTA_Path = RootFolderPath + "UI/ProjectContextWindowUI.uxml";
    private const string ProjectHandlerElementUI_VTA_Path = RootFolderPath + "UI/ProjectHandlerElementUI.uxml";

    private class UIElementFromAssetFactory
    {
        private VisualTreeAsset _visualTreeAsset;
        private StyleSheet _styleSheet;

        public UIElementFromAssetFactory(VisualTreeAsset visualTreeAsset, StyleSheet styleSheet)
        {
            if (visualTreeAsset == null)
                throw new ArgumentNullException($"visualTreeAsset is null");

            _visualTreeAsset = visualTreeAsset;
            _styleSheet = styleSheet;
        }
        public static UIElementFromAssetFactory LoadFrom(in string visualTreeAssetPath, in string styleSheetAssetPath = default)
        {
            return new UIElementFromAssetFactory
            (
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(visualTreeAssetPath),
                styleSheetAssetPath == string.Empty ? null : AssetDatabase.LoadAssetAtPath<StyleSheet>(styleSheetAssetPath)
            );
        }

        public VisualElement CreateVisualElement()
        {
            var visualElement = new VisualElement();
            _visualTreeAsset.CloneTree(visualElement);

            if (_styleSheet != null)
                visualElement.styleSheets.Add(_styleSheet);
            
            return visualElement;
        }
    }

    private static VisualElement _contextManagerExistsPanel;
    private static VisualElement _mainPanel;

    private static ProjectContextManager _projectContextManager;
    private static Dictionary<ContextSwitchHandler, Editor> _handlersUIMGUIEditors = new Dictionary<ContextSwitchHandler, Editor>();

    private static ProjectContextManager ProjectContextManager
    {
        get => _projectContextManager;
        set
        {
            var tmp = _projectContextManager;

            _projectContextManager = value;

            if (_projectContextManager != tmp && value != null)
            {
                if (!TryGetProjectSettingsAsset(out var projectContextsSettings))
                    projectContextsSettings = new ProjectContextsSettings();
                projectContextsSettings.ManagerAssetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_projectContextManager));
                SaveProjectSettingAsset(projectContextsSettings);
            }

            var contextManagerIsNull = _projectContextManager == null;

            _contextManagerExistsPanel.SetDisplay(contextManagerIsNull);
            _mainPanel.SetDisplay(!contextManagerIsNull);
        }
    }

    [SettingsProvider]
    public static SettingsProvider GetSettingsProvider()
    {
        return new SettingsProvider("Project/Project Contexts UI_Elements", SettingsScope.Project)
        {
            label = "Project Contexts UI_Elements",
            activateHandler = (searchContext, windowRootElement) => windowRootElement.Add(GetWindowRootElement()),
            keywords = new string[] { "Context", "Switch" }
        };
    }
    private static VisualElement GetWindowRootElement()
    {
        if (TryGetProjectSettingsAsset(out var projectContextsSettings))
            _projectContextManager = AssetDatabase.LoadAssetAtPath<ProjectContextManager>(AssetDatabase.GUIDToAssetPath(projectContextsSettings.ManagerAssetGUID));

        ProjectContext _currentContext = null;

        #region fetch visual elements
        var rootElement = new VisualElement();
        AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ProjectContextWindowUI_VTA_Path).CloneTree(rootElement);
        var rootUIProvider = new ProjectContextsWindowUIProvider(rootElement);

        var handlerElement_VTA = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ProjectHandlerElementUI_VTA_Path);

        var setttingsDataExistsPanel = rootUIProvider.SettingsDataExistsPanel;
        var contextManagerExistsPanel = rootUIProvider.ContextManagerExistsPanel;
        var mainPanel = rootUIProvider.MainPanel;
        var contextPanel = rootUIProvider.ContextPanel;
        contextPanel.SetDisplay(false);
        #endregion

        #region configure settings-data-exists-panel
        rootUIProvider.CreateNewSettingsDataButton.clicked += () =>
        {
            setttingsDataExistsPanel.SetDisplay(false);
            contextManagerExistsPanel.SetDisplay(ProjectContextManager == null);
            mainPanel.SetDisplay(ProjectContextManager != null);
            SaveProjectSettingAsset(default);
        };
        #endregion
        #region configure manager-exists-panel
        rootUIProvider.CreateNewSettingsDataButton.clicked += () =>
        {
            var managerAssetPath = EditorUtility.SaveFilePanelInProject("Manager asset", "ProjectContextManager", "asset", "message");

            //TODO: check if path not in Assets/ then decline creation

            ProjectContextManager = ScriptableObject.CreateInstance<ProjectContextManager>();
            AssetDatabase.CreateAsset(ProjectContextManager, managerAssetPath);
            AssetDatabase.SaveAssets();
        };
        var contextManagerField = rootUIProvider.ContextManagerField;
        contextManagerField.objectType = typeof(ProjectContextManager);
        contextManagerField.RegisterValueChangedCallback((callback) => ProjectContextManager = callback.newValue as ProjectContextManager);
        #endregion
        #region configure main-panel
        var contextCreatePanel = rootUIProvider.CreateContextPanel;
        var sameContextNameErrorLabel = rootUIProvider.SameContextNameErrorLabel;
        sameContextNameErrorLabel.SetDisplay(false);
        var contextNameField = rootUIProvider.ContextNameField;
        var contextCreateButton = rootUIProvider.CreateNewContextButton;
        contextCreateButton.SetDisplay(false);
        contextNameField.RegisterValueChangedCallback((callback) =>
        {
            if (_projectContextManager != null)
            {
                var isValid = _projectContextManager.ProjectContexts.Where(projectContext => projectContext != null && projectContext.Name == callback.newValue).Count() == 0;
                sameContextNameErrorLabel.SetDisplay(!isValid);
                contextCreateButton.SetDisplay(isValid && callback.newValue != string.Empty);
            }
        });

        var contextsList = rootUIProvider.ContextsList;

        void DisplayContext(ProjectContext context)
        {
            rootUIProvider.ContextNameLabel.text = context.Name;
            rootUIProvider.ContextPanel.SetDisplay(true);
            if (_projectContextManager.IsCurrent(context))
            {
                rootUIProvider.SwitchContextButton.text = "Unswitch";
                rootUIProvider.SwitchContextButton.clickable = null; //clear all listeners
                rootUIProvider.SwitchContextButton.clicked += () =>
                {
                    _projectContextManager.UnswitchFrom(_currentContext);
                    DisplayContextsList();
                    DisplayContext(context); //update context displaying
                };
            }
            else
            {
                rootUIProvider.SwitchContextButton.text = "Switch";
                rootUIProvider.SwitchContextButton.clickable = null; //clear all listeners
                rootUIProvider.SwitchContextButton.clicked += () =>
                {
                    _projectContextManager.SwitchTo(_currentContext);
                    DisplayContextsList();
                    DisplayContext(context); //update context displaying
                };
            }

            void DisplayHandlerList()
            {
                var contextHandlersList = rootUIProvider.ContextHandlersList;
                contextHandlersList.Clear();

                void DisplayHandler(ContextSwitchHandler handler)
                {

                    var root = new VisualElement();
                    handlerElement_VTA.CloneTree(root);
                    contextHandlersList.Add(root);


                    var title = root.Q<Label>("HandlerTitle").text = handler == null ? "This handler is missed" : handler.GetType().Name;

                    root.Q<Button>("HandlerDeleteButton").clicked += () =>
                    {
                        void RemoveHandler()
                        {
                            //delete handler
                            _currentContext.RemoveHandler(handler);
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                            DisplayHandlerList();
                        }
                        if (handler == null)
                        {
                            if (EditorUtility.DisplayDialog("Remove handler confirmation", "Remove missing handler?", "Remove", "Cancel"))
                                RemoveHandler();
                        }
                        else
                            switch (EditorUtility.DisplayDialogComplex("Remove handler confiramtion", "Remove handler or destroy completely?", "Just remove", "Destroy", "Cancel"))
                            {
                                case 0:
                                    RemoveHandler();
                                    break;
                                case 1:
                                    if (AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(_currentContext)))
                                        RemoveHandler();
                                    else
                                        throw new Exception($"Something went wrong while deleting {_currentContext.Name} of asset {_currentContext.name}");
                                    break;
                                    //case 2 do nothing because it is cancelation
                            }
                    };

                    if (handler != null)
                    {
                        var handlerIUMGUIContainer = root.Q<IMGUIContainer>("HandlerSerializeContainer");
                        if (!_handlersUIMGUIEditors.TryGetValue(handler, out var handlerEditor))
                        {
                            handlerEditor = Editor.CreateEditor(handler);
                            _handlersUIMGUIEditors.Add(handler, handlerEditor);
                        }
                        handlerIUMGUIContainer.onGUIHandler += () => handlerEditor.OnInspectorGUI();
                    }
                }
                foreach (var handler in _currentContext.SwitchHandlers)
                    DisplayHandler(handler);
            }
            DisplayHandlerList();
        }
        void DisplayContextsList()
        {
            void AddNewContextListElement(ProjectContext context)
            {
                var contextButton = new Button();
                contextsList.Add(contextButton);

                contextButton.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleLeft);

                var contextUIElementIndex = contextsList.childCount - 1;

                if (context == null)
                {
                    contextButton.text = "Missing context";
                    contextButton.style.color = new Color(1f, 57f / 255f, 57f / 255f);
                    contextButton.clicked += () =>
                    {
                        if (EditorUtility.DisplayDialog("Remove context confiramtion", $"Remove missed context from {_projectContextManager.name}?", "Remove", "Cancel"))
                        {
                            _projectContextManager.Deregistrate(_currentContext);
                            DisplayContextsList();
                        }
                    };
                }
                else
                {
                    contextButton.text = context.Name;
                    contextButton.clicked += () =>
                    {
                        _currentContext = context;
                        DisplayContext(context);
                    };
                }

                if (_projectContextManager.IsCurrent(context))
                    contextButton.AddToClassList("green-button");
            }

            contextsList.Clear();
            //fill context list with existing contexts
            if (ProjectContextManager != null)
                foreach (var context in _projectContextManager.ProjectContexts)
                    AddNewContextListElement(context);
        }

        contextCreateButton.clicked += () =>
        {
            var projectContextPath = EditorUtility.SaveFilePanelInProject("ProjectContext", contextNameField.value, "asset", "message");
            if (projectContextPath != string.Empty)
            {
                var projectContext = ScriptableObject.CreateInstance<ProjectContext>();
                projectContext.Name = contextNameField.value;
                AssetDatabase.CreateAsset(projectContext, projectContextPath);
                _projectContextManager.Registrate(projectContext);
                EditorUtility.SetDirty(_projectContextManager);
                AssetDatabase.SaveAssets();

                contextNameField.value = string.Empty;
                DisplayContextsList();
            }
        };

        rootUIProvider.UpdateContextListButton.clicked += () => DisplayContextsList();
        DisplayContextsList();
        #endregion
        #region configure context-panel
        var handlerSelectorContainer = rootUIProvider.HandlerSelectorContainer;
        var switchHandlerTypesList = GetSwitchHandlersTypes().ToList();
        //for some reason there is no PopupField in UI Builder, so create it from here
        var handlerSelector = new PopupField<Type>(switchHandlerTypesList, 0, GetHandlerLabel, GetHandlerLabel);
        handlerSelectorContainer.Add(handlerSelector);
        rootUIProvider.CreateHandlerButton.clicked += () =>
        {
            if (_currentContext == null)
                return;

            var handlerType = handlerSelector.value;
            var handlerAssetPath = EditorUtility.SaveFilePanelInProject(handlerType.Name, $"{_currentContext.Name}_{handlerType.Name}", "asset", "message");
            if (handlerAssetPath != string.Empty)
            {
                var handler = ScriptableObject.CreateInstance(handlerType) as ContextSwitchHandler;
                _currentContext.AddHandler(handler);
                EditorUtility.SetDirty(_currentContext);
                AssetDatabase.CreateAsset(handler, handlerAssetPath);
                AssetDatabase.SaveAssets();

                DisplayContext(_currentContext); //update current context displaying
            }
        };
        rootUIProvider.DeleteContextButton.clicked += () =>
        {
            void RemoveContext()
            {
                _projectContextManager.Deregistrate(_currentContext);
                DisplayContextsList();
                rootUIProvider.ContextPanel.SetDisplay(false);
            }
            switch (EditorUtility.DisplayDialogComplex("Remove context confiramtion", "Remove context or destroy completely?", "Just remove", "Destroy", "Cancel"))
            {
                case 0:
                    RemoveContext();
                    break;
                case 1:
                    if (AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(_currentContext)))
                    {
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        RemoveContext();
                    }
                    else
                        throw new Exception($"Something went wrong while deleting {_currentContext.Name} of asset {_currentContext.name}");
                    break;
                    //case 2 do nothing because it is cancelation
            }
        };
        #endregion

        //TODO: write method to make full check and enable/disable panels
        //before any - nothing visible
        mainPanel.SetDisplay(false);
        setttingsDataExistsPanel.SetDisplay(false);
        contextManagerExistsPanel.SetDisplay(false);

        mainPanel.SetDisplay(HandleDataExists(setttingsDataExistsPanel, contextManagerExistsPanel));

        return rootElement;
    }
    
    private static bool HandleDataExists(VisualElement settingsDataExistsPanel, VisualElement contextManagerExistsPanel)
    {
        var settingsFileExists = File.Exists(ProjectSettingAssetPath);
        settingsDataExistsPanel.SetDisplay(!settingsFileExists);
        if (!settingsFileExists)
            return false;

        contextManagerExistsPanel.SetDisplay(ProjectContextManager == null);

        return ProjectContextManager != null;
    }

    private static void SaveProjectSettingAsset(in ProjectContextsSettings projectContextsSettings)
    {
        File.WriteAllText(ProjectSettingAssetPath, JsonUtility.ToJson(projectContextsSettings, true));
    }
    private static bool TryGetProjectSettingsAsset(out ProjectContextsSettings projectContextsSettings)
    {
        if (File.Exists(ProjectSettingAssetPath))
        {
            projectContextsSettings = JsonUtility.FromJson<ProjectContextsSettings>(File.ReadAllText(ProjectSettingAssetPath));
            return true;
        }
        projectContextsSettings = default;
        return false;
    }

    private static IEnumerable<Type> GetSwitchHandlersTypes()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(ContextSwitchHandler))));
    }
    private static string GetHandlerLabel(Type handlerType)
    {
        return Attribute.GetCustomAttributes(handlerType)
            .Where(attribute => attribute is SwitchHandlerAttribute)
            .Select(attribute => (attribute as SwitchHandlerAttribute).Label)
            .First();
    }

    private static void SetDisplay(this VisualElement visualElement, in bool value)
    {
        if (visualElement == null)
            throw new ArgumentException(nameof(visualElement));

        visualElement.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
    }

    //TODO: replace with code-gen solution
    #region elements providers
    private abstract class UIProvider
    {
        protected VisualElement _root;

        public UIProvider(VisualElement root) => _root = root;

        public VisualElement Root => _root;
    }
    private class ProjectContextsWindowUIProvider : UIProvider
    {
        public ProjectContextsWindowUIProvider(VisualElement root) : base(root) { }

        public VisualElement SettingsDataExistsPanel => _root.Q("SettingsDataExistsPanel");
        public Button CreateNewSettingsDataButton => _root.Q<Button>("CreateNewSettingsDataButton");
        public VisualElement ContextManagerExistsPanel => _root.Q("ContextManagerExistsPanel");
        public ObjectField ContextManagerField => _root.Q<ObjectField>("ContextManagerField");
        public VisualElement MainPanel => _root.Q("MainPanel");
        public VisualElement CreateContextPanel => _root.Q("CreateContextPanel");
        public TextField ContextNameField => _root.Q<TextField>("ContextNameField");
        public Button CreateNewContextButton => _root.Q<Button>("CreateNewContextButton");
        public Label SameContextNameErrorLabel => _root.Q<Label>("SameContextNameErrorLabel");
        public ScrollView ContextsList => _root.Q<ScrollView>("ContextsList");
        public VisualElement ContextPanel => _root.Q("ContextPanel");
        public Label ContextNameLabel => ContextPanel.Q<Label>("ContextNameLabel");
        public VisualElement HandlerSelectorContainer => _root.Q("HandlerSelectorContainer");
        public Button CreateHandlerButton => _root.Q<Button>("CreateHandlerButton");
        public VisualElement ContextHandlersList => _root.Q("ContextHandlersList");
        public Button DeleteContextButton => _root.Q<Button>("DeleteContextButton");
        public Button SwitchContextButton => _root.Q<Button>("SwitchContextButton");
        public Button UpdateContextListButton => _root.Q<Button>("UpdateContextListButton");
    }
    #endregion
}
