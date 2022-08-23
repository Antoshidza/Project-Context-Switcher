using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectContextSwitcher
{
    public static class ProjectContextSwitcherSettingsWindow
    {
        private struct SwithHandlerData
        {
            public Type Type;
            public string Label;
            public int Order;
        }
        private struct HandlerOrderComparer : IComparer<Type>
        {
            public int Compare(Type x, Type y) => GetSwitchHandlerData(x).Order.CompareTo(GetSwitchHandlerData(y).Order);
        }

        private const string ProjectSettingAssetPath = "ProjectSettings/ProjectContextsSettings.asset";
        private const float VerticalSpacing = 10f;

        private static string _newContextName = string.Empty;
        private static int _selectedContextIndex = -1;
        private static int _selectedHandlerTypeIndex = -1;
        private static ProjectContextManager _projectContextManager;
        private static SwithHandlerData[] _switchHanlers;

        [SettingsProvider]
        public static SettingsProvider GetSettingsProvider()
        {
            GatherSwitchHandlerTypes();

            if (TryGetProjectSettingsAsset(out var projectContextsSettings))
                _projectContextManager = AssetDatabase.LoadAssetAtPath<ProjectContextManager>(AssetDatabase.GUIDToAssetPath(projectContextsSettings.ManagerAssetGUID));

            return new SettingsProvider("Project/Project Contexts", SettingsScope.Project)
            {
                label = "Project Contexts",
                guiHandler = (searchContext) =>
                {
                    GUILayout.Space(VerticalSpacing);

                    //After this check we ensure that settings exists in ProjectSettings folder and there is assigned ProjectContextManager
                    if (!HandleSettingsDataExists())
                        return;

                    GUILayout.Space(VerticalSpacing);

                    var projectContexts = _projectContextManager.ProjectContexts;

                    if (projectContexts == null)
                        return;

                    DrawContextCreateField(projectContexts);

                    GUILayout.Space(VerticalSpacing);

                    DrawContextsList(projectContexts);

                    GUILayout.Space(VerticalSpacing);

                    if (_selectedContextIndex != -1)
                        DrawProjectContext(projectContexts.ElementAt(_selectedContextIndex));
                },
                keywords = new string[] { "Context", "Switch" }
            };
        }
        private static bool HandleSettingsDataExists()
        {
            var settingsFileExists = File.Exists(ProjectSettingAssetPath);

            if (!settingsFileExists)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("There is no context data in project for now");
                if (GUILayout.Button("Create new", GUILayout.ExpandWidth(false)))
                    SaveProjectSettingAsset(default);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                return false;
            }

            if (_projectContextManager == null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                var labelWordWrapTmp = GUI.skin.label.wordWrap;
                GUI.skin.label.wordWrap = true;
                GUILayout.Label("You need to assign Project Context Manager to start edit/switch contexts. Chose from project or create one.");
                GUI.skin.label.wordWrap = labelWordWrapTmp;
                if (GUILayout.Button("Create new", GUILayout.ExpandWidth(false)))
                {
                    var managerAssetPath = EditorUtility.SaveFilePanelInProject("Manager asset", "ProjectContextManager", "asset", "message");

                    //TODO: check if path not in Assets/ then decline creation

                    _projectContextManager = ScriptableObject.CreateInstance<ProjectContextManager>();
                    AssetDatabase.CreateAsset(_projectContextManager, managerAssetPath);
                    AssetDatabase.SaveAssets();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            _projectContextManager = EditorGUILayout.ObjectField("Context Manager", _projectContextManager, typeof(ProjectContextManager), false) as ProjectContextManager;

            return _projectContextManager != null;
        }
        private static void DrawContextCreateField(IEnumerable<ProjectContext> projectContexts)
        {
            GUILayout.BeginHorizontal();
            _newContextName = EditorGUILayout.TextField("Create Context", _newContextName, GUILayout.ExpandWidth(true));
            if (_newContextName != string.Empty)
            {
                if (projectContexts.Where(projectContext => projectContext.Name == _newContextName).Count() != 0)
                    EditorGUILayout.HelpBox("There is already one context with this name, please, use unique name", MessageType.Error);
                else if (GUILayout.Button("Create", GUILayout.ExpandWidth(false)))
                {
                    var projectContextPath = EditorUtility.SaveFilePanelInProject("ProjectContext", _newContextName, "asset", "message");
                    if (projectContextPath != string.Empty)
                    {
                        var projectContext = ScriptableObject.CreateInstance<ProjectContext>();
                        projectContext.Name = _newContextName;
                        AssetDatabase.CreateAsset(projectContext, projectContextPath);
                        _projectContextManager.Registrate(projectContext);
                        EditorUtility.SetDirty(_projectContextManager);
                        AssetDatabase.SaveAssets();
                    }

                    _newContextName = string.Empty;
                }
            }
            GUILayout.EndHorizontal();
        }
        private static void DrawContextsList(IEnumerable<ProjectContext> projectContexts)
        {
            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            var i = 0;
            foreach (var projectContext in projectContexts)
            {
                if (projectContext == null)
                    //TODO: clear manager
                    continue;

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("x", GUILayout.ExpandWidth(false)) && EditorUtility.DisplayDialog($"Delete {projectContext.Name}", "Are you sure you want delete this context?", "confirm", "cancel"))
                {
                    _projectContextManager.Deregistrate(projectContext);
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(projectContext));
                }
                if (GUILayout.Button("edit", GUILayout.ExpandWidth(false)))
                    _selectedContextIndex = i;
                if (GUILayout.Button("switch", GUILayout.ExpandWidth(false)) && EditorUtility.DisplayDialog($"Switch project context", $"Are you sure you want switch project to {projectContext.Name}?", "confirm", "cancel"))
                {
                    _projectContextManager.SwitchTo(i);
                    EditorUtility.SetDirty(_projectContextManager);
                    AssetDatabase.SaveAssets();
                    //AssetDatabase.Refresh();
                }

                GUI.skin.label.normal.textColor = _projectContextManager.IsCurrent(projectContext) ? Color.green : Color.white;
                GUILayout.Label(projectContext.Name);
                GUI.skin.label.normal.textColor = Color.white;

                GUILayout.EndHorizontal();
                i++;
            }
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
        }
        private static void DrawProjectContext(ProjectContext projectContext)
        {
            GUILayout.BeginHorizontal();
            var labelStyle = new GUIStyle() { fontSize = 20, fontStyle = FontStyle.Bold };
            labelStyle.normal.textColor = Color.white;
            EditorGUILayout.LabelField(projectContext.Name, labelStyle);
            _selectedHandlerTypeIndex = EditorGUILayout.Popup(_selectedHandlerTypeIndex, _switchHanlers.Select(handler => handler.Label).ToArray());
            if (_selectedHandlerTypeIndex >= 0 && GUILayout.Button("add handler", GUILayout.ExpandWidth(false)))
            {
                var handlerType = _switchHanlers[_selectedHandlerTypeIndex].Type;
                var handlerAssetPath = EditorUtility.SaveFilePanelInProject(handlerType.Name, $"{projectContext.Name}_{handlerType.Name}", "asset", "message");
                if (handlerAssetPath != string.Empty)
                {
                    var handler = ScriptableObject.CreateInstance(handlerType) as ContextSwitchHandler;
                    projectContext.AddHandler(handler);
                    EditorUtility.SetDirty(projectContext);
                    AssetDatabase.CreateAsset(handler, handlerAssetPath);
                    AssetDatabase.SaveAssets();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(VerticalSpacing);

            var switchHandlers = projectContext.SwitchHandlers;
            foreach (var switchHandler in switchHandlers)
            {
                if (switchHandler == null)
                    //TODO: clear context
                    continue;
                labelStyle.fontSize = 15;
                EditorGUILayout.LabelField(GetSwitchHandlerData(switchHandler.GetType()).Label, labelStyle);
                Editor.CreateEditor(switchHandler).OnInspectorGUI();
                GUILayout.Space(VerticalSpacing);
            }
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
        private static void GatherSwitchHandlerTypes()
        {
            _switchHanlers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes()
                    .Where(type => typeof(ContextSwitchHandler).IsAssignableFrom(type)))
                .SelectMany(type => Attribute.GetCustomAttributes(type)
                    .Where(attribute => attribute is SwitchHandlerAttribute)
                    .Select(attribute =>
                    {
                        var switchHandlerAttribute = attribute as SwitchHandlerAttribute;
                        return new SwithHandlerData { Label = switchHandlerAttribute.Label, Type = type, Order = switchHandlerAttribute.Order };
                    }))
                .ToArray();
        }
        private static SwithHandlerData GetSwitchHandlerData(Type type) => _switchHanlers.Where(data => data.Type == type).First();
    }
}