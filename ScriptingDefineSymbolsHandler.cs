using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProjectContextSwitcher
{
    [SwitchHandler(Label = "Scripting Define Symbols")]
    [CreateAssetMenu(menuName = "SciptingDefineSymbolsHandler", fileName = "NewSciptingDefineSymbolsHandler")]
    public class ScriptingDefineSymbolsHandler : ContextSwitchHandler
    {
        [SerializeField] private string[] _symbols;

        public override void Switch(out string report)
        {
            var symbolsStringTmp = GetScriptingDefineSymbolsString();
            var currentSymbols = GetScriptingDefineSymbols().ToList();
            currentSymbols.AddRange(_symbols);
            SetScriptingDefineSymbols(currentSymbols);

            report = $"Scripting Define Symbols was: {symbolsStringTmp}\nSymbols was added:\n\t{string.Join("\n\t", _symbols)}\nScripting Define Symbols now: {GetScriptingDefineSymbolsString()}";
        }
        public override void Unswitch(out string report)
        {
            var symbolsStringTmp = GetScriptingDefineSymbolsString();
            var currentSymbols = GetScriptingDefineSymbols();
            SetScriptingDefineSymbols(currentSymbols.Except(_symbols));
            report = $"Scripting Define Symbols was: {symbolsStringTmp}\nSymbols was removed:\n\t{string.Join("\n\t", _symbols)}\nScripting Define Symbols now: {GetScriptingDefineSymbolsString()}";
        }

        public override bool ValidateProject()
        {
            var currentSymbols = GetScriptingDefineSymbols();
            return _symbols.All(symbol => currentSymbols.Contains(symbol));
        }
        private void SetScriptingDefineSymbols(IEnumerable<string> symbols) => PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", symbols));
        private string GetScriptingDefineSymbolsString() => PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        private string[] GetScriptingDefineSymbols() => GetScriptingDefineSymbolsString().Split(';');
    }
}