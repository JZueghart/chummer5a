/*  This file is part of Chummer5a.
 *
 *  Chummer5a is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Chummer5a is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Chummer5a.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  You can obtain the full source code for Chummer5a at
 *  https://github.com/chummer5a/chummer5a
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;

namespace Chummer
{
    public sealed class StoryBuilder : IHasLockObject
    {
        private readonly LockingDictionary<string, string> _dicPersistence = new LockingDictionary<string, string>();
        private readonly Character _objCharacter;

        public StoryBuilder(Character objCharacter)
        {
            _objCharacter = objCharacter ?? throw new ArgumentNullException(nameof(objCharacter));
            _dicPersistence.TryAdd("metatype", _objCharacter.Metatype.ToLowerInvariant());
            _dicPersistence.TryAdd("metavariant", _objCharacter.Metavariant.ToLowerInvariant());
        }

        public async ValueTask<string> GetStory(string strLanguage = "", CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(strLanguage))
                strLanguage = GlobalSettings.Language;

            using (await EnterReadLock.EnterAsync(LockObject, token).ConfigureAwait(false))
            {
                //Little bit of data required for following steps
                XmlDocument xmlDoc = await _objCharacter.LoadDataAsync("lifemodules.xml", strLanguage, token: token)
                                                        .ConfigureAwait(false);
                XPathNavigator xdoc = await _objCharacter
                                            .LoadDataXPathAsync("lifemodules.xml", strLanguage, token: token)
                                            .ConfigureAwait(false);

                //Generate list of all life modules (xml, we don't save required data to quality) this character has
                List<XmlNode> modules = new List<XmlNode>(10);

                foreach (Quality quality in _objCharacter.Qualities)
                {
                    if (quality.Type == QualityType.LifeModule)
                    {
                        modules.Add(Quality.GetNodeOverrideable(quality.SourceIDString, xmlDoc));
                    }
                }

                //Sort the list (Crude way, but have to do)
                for (int i = 0; i < modules.Count; i++)
                {
                    string stageName = (await xdoc
                                              .SelectSingleNodeAndCacheExpressionAsync("chummer/stages/stage[@order = "
                                                  + (i <= 4
                                                      ? (i + 1).ToString(GlobalSettings.InvariantCultureInfo)
                                                               .CleanXPath()
                                                      : "\"5\"") + ']', token: token).ConfigureAwait(false))?.Value;
                    int j;
                    for (j = i; j < modules.Count; j++)
                    {
                        if (modules[j]["stage"]?.InnerText == stageName)
                            break;
                    }

                    if (j != i && j < modules.Count)
                    {
                        (modules[i], modules[j]) = (modules[j], modules[i]);
                    }
                }

                string[] story = new string[modules.Count];
                Task<string>[] atskStoryTasks = new Task<string>[modules.Count];
                XPathNavigator xmlBaseMacrosNode = await xdoc
                                                         .SelectSingleNodeAndCacheExpressionAsync(
                                                             "/chummer/storybuilder/macros", token: token)
                                                         .ConfigureAwait(false);
                //Actually "write" the story
                for (int i = 0; i < modules.Count; ++i)
                {
                    int intLocal = i;
                    atskStoryTasks[i] = Task.Run(async () =>
                    {
                        using (new FetchSafelyFromPool<StringBuilder>(Utils.StringBuilderPool,
                                                                      out StringBuilder sbdTemp))
                        {
                            return (await Write(sbdTemp, modules[intLocal]["story"]?.InnerText ?? string.Empty, 5,
                                                xmlBaseMacrosNode, token).ConfigureAwait(false)).ToString();
                        }
                    }, token);
                }

                await Task.WhenAll(atskStoryTasks).ConfigureAwait(false);

                for (int i = 0; i < modules.Count; ++i)
                {
                    story[i] = await atskStoryTasks[i].ConfigureAwait(false);
                }

                return string.Join(Environment.NewLine + Environment.NewLine, story);
            }
        }

        private async Task<StringBuilder> Write(StringBuilder story, string innerText, int levels, XPathNavigator xmlBaseMacrosNode, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (levels <= 0)
                return story;
            using (await EnterReadLock.EnterAsync(LockObject, token).ConfigureAwait(false))
            {
                int startingLength = story.Length;

                IEnumerable<string> words;
                if (innerText.StartsWith('$') && innerText.IndexOf(' ') < 0)
                {
                    words = (await Macro(innerText, xmlBaseMacrosNode, token).ConfigureAwait(false)).SplitNoAlloc(
                        ' ', '\n', '\r', '\t');
                }
                else
                {
                    words = innerText.SplitNoAlloc(' ', '\n', '\r', '\t');
                }

                bool mfix = false;
                foreach (string word in words)
                {
                    if (string.IsNullOrWhiteSpace(word))
                        continue;
                    token.ThrowIfCancellationRequested();
                    string trim = word.Trim();

                    if (trim.StartsWith('$'))
                    {
                        if (trim.StartsWith("$DOLLAR", StringComparison.Ordinal))
                        {
                            story.Append('$');
                        }
                        else
                        {
                            //if (story.Length > 0 && story[story.Length - 1] == ' ') story.Length--;
                            await Write(story, trim, --levels, xmlBaseMacrosNode, token).ConfigureAwait(false);
                        }

                        mfix = true;
                    }
                    else
                    {
                        if (story.Length != startingLength && !mfix)
                        {
                            story.Append(' ');
                        }
                        else
                        {
                            mfix = false;
                        }

                        story.Append(trim);
                    }
                }

                return story;
            }
        }

        public async ValueTask<string> Macro(string innerText, XPathNavigator xmlBaseMacrosNode, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(innerText))
                return string.Empty;
            string endString = innerText.ToLowerInvariant().Substring(1).TrimEnd(',', '.');
            string macroName, macroPool;
            if (endString.Contains('_'))
            {
                string[] split = endString.Split('_');
                macroName = split[0];
                macroPool = split[1];
            }
            else
            {
                macroName = macroPool = endString;
            }

            using (await EnterReadLock.EnterAsync(LockObject, token).ConfigureAwait(false))
            {
                switch (macroName)
                {
                    //$DOLLAR is defined elsewhere to prevent recursive calling
                    case "street":
                        return !string.IsNullOrEmpty(_objCharacter.Alias) ? _objCharacter.Alias : "Alias ";

                    case "real":
                        return !string.IsNullOrEmpty(_objCharacter.Name) ? _objCharacter.Name : "Unnamed John Doe ";

                    case "year" when int.TryParse(_objCharacter.Age, out int year):
                        return int.TryParse(macroPool, out int age)
                            ? (DateTime.UtcNow.Year + 62 + age - year).ToString(GlobalSettings.CultureInfo)
                            : (DateTime.UtcNow.Year + 62 - year).ToString(GlobalSettings.CultureInfo);

                    case "year":
                        return "(ERROR PARSING \"" + _objCharacter.Age + "\")";
                }

                //Did not meet predefined macros, check user defined

                XPathNavigator xmlUserMacroNode = xmlBaseMacrosNode?.SelectSingleNode(macroName);

                if (xmlUserMacroNode != null)
                {
                    XPathNavigator xmlUserMacroFirstChild
                        = xmlUserMacroNode.SelectChildren(XPathNodeType.Element).Current;
                    if (xmlUserMacroFirstChild != null)
                    {
                        //Already defined, no need to do anything fancy
                        (bool blnSuccess, string strSelectedNodeName)
                            = await _dicPersistence.TryGetValueAsync(macroPool, token).ConfigureAwait(false);
                        if (!blnSuccess)
                        {
                            switch (xmlUserMacroFirstChild.Name)
                            {
                                case "random":
                                {
                                    XPathNodeIterator xmlPossibleNodeList = await xmlUserMacroFirstChild
                                        .SelectAndCacheExpressionAsync("./*[not(self::default)]", token: token)
                                        .ConfigureAwait(false);
                                    if (xmlPossibleNodeList.Count > 0)
                                    {
                                        int intUseIndex = xmlPossibleNodeList.Count > 1
                                            ? await GlobalSettings.RandomGenerator
                                                                  .NextModuloBiasRemovedAsync(
                                                                      xmlPossibleNodeList.Count, token: token)
                                                                  .ConfigureAwait(false)
                                            : 0;
                                        int i = 0;
                                        foreach (XPathNavigator xmlLoopNode in xmlPossibleNodeList)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            if (i == intUseIndex)
                                            {
                                                strSelectedNodeName = xmlLoopNode.Name;
                                                break;
                                            }

                                            ++i;
                                        }
                                    }

                                    break;
                                }
                                case "persistent":
                                {
                                    //Any node not named
                                    XPathNodeIterator xmlPossibleNodeList = await xmlUserMacroFirstChild
                                        .SelectAndCacheExpressionAsync("./*[not(self::default)]", token: token)
                                        .ConfigureAwait(false);
                                    if (xmlPossibleNodeList.Count > 0)
                                    {
                                        int intUseIndex = xmlPossibleNodeList.Count > 1
                                            ? await GlobalSettings.RandomGenerator
                                                                  .NextModuloBiasRemovedAsync(
                                                                      xmlPossibleNodeList.Count, token: token)
                                                                  .ConfigureAwait(false)
                                            : 0;
                                        int i = 0;
                                        foreach (XPathNavigator xmlLoopNode in xmlPossibleNodeList)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            if (i == intUseIndex)
                                            {
                                                strSelectedNodeName = xmlLoopNode.Name;
                                                break;
                                            }

                                            ++i;
                                        }

                                        string strToAdd = strSelectedNodeName;
                                        strSelectedNodeName = await _dicPersistence
                                                                    .AddOrGetAsync(macroPool, x => strToAdd, token)
                                                                    .ConfigureAwait(false);
                                    }

                                    break;
                                }
                                default:
                                    return "(Formating error in $DOLLAR" + macroName + ')';
                            }
                        }

                        if (!string.IsNullOrEmpty(strSelectedNodeName))
                        {
                            string strSelected = xmlUserMacroFirstChild.SelectSingleNode(strSelectedNodeName)?.Value;
                            if (!string.IsNullOrEmpty(strSelected))
                                return strSelected;
                        }

                        string strDefault = (await xmlUserMacroFirstChild
                                                   .SelectSingleNodeAndCacheExpressionAsync("default", token: token)
                                                   .ConfigureAwait(false))?.Value;
                        if (!string.IsNullOrEmpty(strDefault))
                        {
                            return strDefault;
                        }

                        return "(Unknown key " + macroPool + " in $DOLLAR" + macroName + ')';
                    }

                    return xmlUserMacroNode.Value;
                }

                return "(Unknown Macro $DOLLAR" + innerText.Substring(1) + ')';
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            using (LockObject.EnterWriteLock())
                _dicPersistence.Dispose();
            LockObject.Dispose();
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync().ConfigureAwait(false);
            try
            {
                await _dicPersistence.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }

            await LockObject.DisposeAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public AsyncFriendlyReaderWriterLock LockObject { get; } = new AsyncFriendlyReaderWriterLock();
    }
}
