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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using Chummer.Backend.Uniques;

namespace Chummer
{
    public partial class SpiritControl : UserControl
    {
        private readonly Spirit _objSpirit;
        private bool _blnLoading = true;

        // Events.
        public event EventHandler ContactDetailChanged;

        public event EventHandler DeleteSpirit;

        #region Control Events

        public SpiritControl(Spirit objSpirit)
        {
            _objSpirit = objSpirit;
            InitializeComponent();

            Disposed += (sender, args) => UnbindSpiritControl();

            this.UpdateLightDarkMode();
            this.TranslateWinForm();
            foreach (ToolStripItem tssItem in cmsSpirit.Items)
            {
                tssItem.UpdateLightDarkMode();
                tssItem.TranslateToolStripItemsRecursively();
            }
        }

        private async void SpiritControl_Load(object sender, EventArgs e)
        {
            bool blnIsSpirit = _objSpirit.EntityType == SpiritType.Spirit;
            await nudForce.DoOneWayDataBindingAsync("Enabled", _objSpirit.CharacterObject, nameof(Character.Created)).ConfigureAwait(false);
            await chkBound.DoDataBindingAsync("Checked", _objSpirit, nameof(Spirit.Bound)).ConfigureAwait(false);
            await chkBound.DoOneWayDataBindingAsync("Enabled", _objSpirit.CharacterObject, nameof(Character.Created)).ConfigureAwait(false);
            await cboSpiritName.DoDataBindingAsync("Text", _objSpirit, nameof(Spirit.Name)).ConfigureAwait(false);
            await txtCritterName.DoDataBindingAsync("Text", _objSpirit, nameof(Spirit.CritterName)).ConfigureAwait(false);
            await txtCritterName.DoOneWayDataBindingAsync("Enabled", _objSpirit, nameof(Spirit.NoLinkedCharacter)).ConfigureAwait(false);
            await nudForce.DoOneWayDataBindingAsync("Maximum", _objSpirit.CharacterObject, blnIsSpirit ? nameof(Character.MaxSpiritForce) : nameof(Character.MaxSpriteLevel)).ConfigureAwait(false);
            await nudServices.DoDataBindingAsync("Value", _objSpirit, nameof(Spirit.ServicesOwed)).ConfigureAwait(false);
            await nudForce.DoDataBindingAsync("Value", _objSpirit, nameof(Spirit.Force)).ConfigureAwait(false);
            await chkFettered.DoOneWayDataBindingAsync("Enabled", _objSpirit, nameof(Spirit.AllowFettering)).ConfigureAwait(false);
            await chkFettered.DoDataBindingAsync("Checked", _objSpirit, nameof(Spirit.Fettered)).ConfigureAwait(false);
            if (blnIsSpirit)
            {
                string strText = await LanguageManager.GetStringAsync("Label_Spirit_Force").ConfigureAwait(false);
                await lblForce.DoThreadSafeAsync(x => x.Text = strText).ConfigureAwait(false);
                string strText2 = await LanguageManager.GetStringAsync("Checkbox_Spirit_Bound").ConfigureAwait(false);
                await chkBound.DoThreadSafeAsync(x => x.Text = strText2).ConfigureAwait(false);
                await cmdLink.SetToolTipTextAsync(await LanguageManager.GetStringAsync(!string.IsNullOrEmpty(_objSpirit.FileName) ? "Tip_Spirit_OpenFile" : "Tip_Spirit_LinkSpirit").ConfigureAwait(false)).ConfigureAwait(false);
                string strTooltip = await LanguageManager.GetStringAsync("Tip_Spirit_EditNotes").ConfigureAwait(false);
                if (!string.IsNullOrEmpty(_objSpirit.Notes))
                    strTooltip += Environment.NewLine + Environment.NewLine + _objSpirit.Notes;
                await cmdNotes.SetToolTipTextAsync(strTooltip.WordWrap()).ConfigureAwait(false);
            }
            else
            {
                string strText = await LanguageManager.GetStringAsync("Label_Sprite_Rating").ConfigureAwait(false);
                await lblForce.DoThreadSafeAsync(x => x.Text = strText).ConfigureAwait(false);
                string strText2 = await LanguageManager.GetStringAsync("Label_Sprite_TasksOwed").ConfigureAwait(false);
                await lblServices.DoThreadSafeAsync(x => x.Text = strText2).ConfigureAwait(false);
                string strText3 = await LanguageManager.GetStringAsync("Label_Sprite_Registered").ConfigureAwait(false);
                await chkBound.DoThreadSafeAsync(x => x.Text = strText3).ConfigureAwait(false);
                string strText4 = await LanguageManager.GetStringAsync("Checkbox_Sprite_Pet").ConfigureAwait(false);
                await chkFettered.DoThreadSafeAsync(x => x.Text = strText4).ConfigureAwait(false);
                await cmdLink.SetToolTipTextAsync(await LanguageManager.GetStringAsync(!string.IsNullOrEmpty(_objSpirit.FileName) ? "Tip_Sprite_OpenFile" : "Tip_Sprite_LinkSpirit").ConfigureAwait(false)).ConfigureAwait(false);
                string strTooltip = await LanguageManager.GetStringAsync("Tip_Sprite_EditNotes").ConfigureAwait(false);
                if (!string.IsNullOrEmpty(_objSpirit.Notes))
                    strTooltip += Environment.NewLine + Environment.NewLine + _objSpirit.Notes;
                await cmdNotes.SetToolTipTextAsync(strTooltip.WordWrap()).ConfigureAwait(false);
            }

            IAsyncDisposable objLocker = await _objSpirit.CharacterObject.LockObject.EnterWriteLockAsync().ConfigureAwait(false);
            try
            {
                _objSpirit.CharacterObject.PropertyChanged += RebuildSpiritListOnTraditionChange;
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }

            _blnLoading = false;
        }

        public void UnbindSpiritControl()
        {
            using (_objSpirit.CharacterObject.LockObject.EnterWriteLock())
                _objSpirit.CharacterObject.PropertyChanged -= RebuildSpiritListOnTraditionChange;

            foreach (Control objControl in Controls)
            {
                objControl.DataBindings.Clear();
            }
        }

        private void chkFettered_CheckedChanged(object sender, EventArgs e)
        {
            // Raise the ContactDetailChanged Event when the Checkbox's Checked status changes.
            // The entire SpiritControl is passed as an argument so the handling event can evaluate its contents.
            if (!_blnLoading)
                ContactDetailChanged?.Invoke(this, e);
        }

        private void nudServices_ValueChanged(object sender, EventArgs e)
        {
            // Raise the ContactDetailChanged Event when the NumericUpDown's Value changes.
            // The entire SpiritControl is passed as an argument so the handling event can evaluate its contents.
            if (!_blnLoading)
                ContactDetailChanged?.Invoke(this, e);
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            // Raise the DeleteSpirit Event when the user has confirmed their desire to delete the Spirit.
            // The entire SpiritControl is passed as an argument so the handling event can evaluate its contents.
            DeleteSpirit?.Invoke(this, e);
        }

        private void nudForce_ValueChanged(object sender, EventArgs e)
        {
            // Raise the ContactDetailChanged Event when the NumericUpDown's Value changes.
            // The entire SpiritControl is passed as an argument so the handling event can evaluate its contents.
            if (!_blnLoading)
                ContactDetailChanged?.Invoke(this, e);
        }

        private void chkBound_CheckedChanged(object sender, EventArgs e)
        {
            // Raise the ContactDetailChanged Event when the Checkbox's Checked status changes.
            // The entire SpiritControl is passed as an argument so the handling event can evaluate its contents.
            if (!_blnLoading)
                ContactDetailChanged?.Invoke(this, e);
        }

        private void cboSpiritName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_blnLoading)
                ContactDetailChanged?.Invoke(this, e);
        }

        private void txtCritterName_TextChanged(object sender, EventArgs e)
        {
            if (!_blnLoading)
                ContactDetailChanged?.Invoke(this, e);
        }

        private async void tsContactOpen_Click(object sender, EventArgs e)
        {
            if (_objSpirit.LinkedCharacter != null)
            {
                Character objOpenCharacter = await Program.OpenCharacters.ContainsAsync(_objSpirit.LinkedCharacter).ConfigureAwait(false)
                    ? _objSpirit.LinkedCharacter
                    : null;
                CursorWait objCursorWait = await CursorWait.NewAsync(ParentForm).ConfigureAwait(false);
                try
                {
                    if (objOpenCharacter == null)
                    {
                        using (ThreadSafeForm<LoadingBar> frmLoadingBar
                               = await Program.CreateAndShowProgressBarAsync(
                                   _objSpirit.LinkedCharacter.FileName, Character.NumLoadingSections).ConfigureAwait(false))
                            objOpenCharacter = await Program.LoadCharacterAsync(
                                _objSpirit.LinkedCharacter.FileName, frmLoadingBar: frmLoadingBar.MyForm).ConfigureAwait(false);
                    }

                    if (!await Program.SwitchToOpenCharacter(objOpenCharacter).ConfigureAwait(false))
                        await Program.OpenCharacter(objOpenCharacter).ConfigureAwait(false);
                }
                finally
                {
                    await objCursorWait.DisposeAsync().ConfigureAwait(false);
                }
            }
            else
            {
                bool blnUseRelative = false;

                // Make sure the file still exists before attempting to load it.
                if (!File.Exists(_objSpirit.FileName))
                {
                    bool blnError = false;
                    // If the file doesn't exist, use the relative path if one is available.
                    if (string.IsNullOrEmpty(_objSpirit.RelativeFileName))
                        blnError = true;
                    else if (!File.Exists(Path.GetFullPath(_objSpirit.RelativeFileName)))
                        blnError = true;
                    else
                        blnUseRelative = true;

                    if (blnError)
                    {
                        Program.ShowScrollableMessageBox(
                            string.Format(GlobalSettings.CultureInfo,
                                          await LanguageManager.GetStringAsync("Message_FileNotFound")
                                                               .ConfigureAwait(false), _objSpirit.FileName),
                            await LanguageManager.GetStringAsync("MessageTitle_FileNotFound").ConfigureAwait(false),
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                string strFile = blnUseRelative ? Path.GetFullPath(_objSpirit.RelativeFileName) : _objSpirit.FileName;
                Process.Start(strFile);
            }
        }

        private async void tsRemoveCharacter_Click(object sender, EventArgs e)
        {
            // Remove the file association from the Contact.
            if (Program.ShowScrollableMessageBox(
                    await LanguageManager.GetStringAsync("Message_RemoveCharacterAssociation").ConfigureAwait(false),
                    await LanguageManager.GetStringAsync("MessageTitle_RemoveCharacterAssociation")
                                         .ConfigureAwait(false), MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                == DialogResult.Yes)
            {
                _objSpirit.FileName = string.Empty;
                _objSpirit.RelativeFileName = string.Empty;
                string strText = await LanguageManager.GetStringAsync(
                                                          _objSpirit.EntityType == SpiritType.Spirit
                                                              ? "Tip_Spirit_LinkSpirit"
                                                              : "Tip_Sprite_LinkSprite")
                                                      .ConfigureAwait(false);
                await cmdLink.SetToolTipTextAsync(strText).ConfigureAwait(false);

                // Set the relative path.
                Uri uriApplication = new Uri(Utils.GetStartupPath);
                Uri uriFile = new Uri(_objSpirit.FileName);
                Uri uriRelative = uriApplication.MakeRelativeUri(uriFile);
                _objSpirit.RelativeFileName = "../" + uriRelative;

                ContactDetailChanged?.Invoke(this, e);
            }
        }

        private async void tsAttachCharacter_Click(object sender, EventArgs e)
        {
            string strFileName = string.Empty;
            string strFilter = await LanguageManager.GetStringAsync("DialogFilter_Chummer").ConfigureAwait(false) + '|'
                +
                await LanguageManager.GetStringAsync("DialogFilter_Chum5").ConfigureAwait(false) + '|' +
                await LanguageManager.GetStringAsync("DialogFilter_Chum5lz").ConfigureAwait(false) + '|' +
                await LanguageManager.GetStringAsync("DialogFilter_All").ConfigureAwait(false);
            // Prompt the user to select a save file to associate with this Contact.
            // Prompt the user to select a save file to associate with this Contact.
            DialogResult eResult = await this.DoThreadSafeFuncAsync(x =>
            {
                using (OpenFileDialog dlgOpenFile = new OpenFileDialog())
                {
                    dlgOpenFile.Filter = strFilter;
                    if (!string.IsNullOrEmpty(_objSpirit.FileName) && File.Exists(_objSpirit.FileName))
                    {
                        dlgOpenFile.InitialDirectory = Path.GetDirectoryName(_objSpirit.FileName);
                        dlgOpenFile.FileName = Path.GetFileName(_objSpirit.FileName);
                    }

                    DialogResult eReturn = dlgOpenFile.ShowDialog(x);
                    strFileName = dlgOpenFile.FileName;
                    return eReturn;
                }
            }).ConfigureAwait(false);

            if (eResult != DialogResult.OK)
                return;
            _objSpirit.FileName = strFileName;
            string strText = await LanguageManager.GetStringAsync(
                _objSpirit.EntityType == SpiritType.Spirit ? "Tip_Spirit_OpenFile" : "Tip_Sprite_OpenFile").ConfigureAwait(false);
            await cmdLink.SetToolTipTextAsync(strText).ConfigureAwait(false);
            ContactDetailChanged?.Invoke(this, e);
        }

        private async void tsCreateCharacter_Click(object sender, EventArgs e)
        {
            string strSpiritName = await cboSpiritName.DoThreadSafeFuncAsync(x => x.SelectedValue?.ToString()).ConfigureAwait(false);
            if (string.IsNullOrEmpty(strSpiritName))
            {
                Program.ShowScrollableMessageBox(
                    await LanguageManager.GetStringAsync("Message_SelectCritterType").ConfigureAwait(false),
                    await LanguageManager.GetStringAsync("MessageTitle_SelectCritterType").ConfigureAwait(false),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            await CreateCritter(strSpiritName, nudForce.ValueAsInt).ConfigureAwait(false);
        }

        private void cmdLink_Click(object sender, EventArgs e)
        {
            // Determine which options should be shown based on the FileName value.
            if (!string.IsNullOrEmpty(_objSpirit.FileName))
            {
                tsAttachCharacter.Visible = false;
                tsCreateCharacter.Visible = false;
                tsContactOpen.Visible = true;
                tsRemoveCharacter.Visible = true;
            }
            else
            {
                tsAttachCharacter.Visible = true;
                tsCreateCharacter.Visible = true;
                tsContactOpen.Visible = false;
                tsRemoveCharacter.Visible = false;
            }
            cmsSpirit.Show(cmdLink, cmdLink.Left - 646, cmdLink.Top);
        }

        private async void cmdNotes_Click(object sender, EventArgs e)
        {
            using (ThreadSafeForm<EditNotes> frmSpiritNotes = await ThreadSafeForm<EditNotes>.GetAsync(() => new EditNotes(_objSpirit.Notes, _objSpirit.NotesColor)).ConfigureAwait(false))
            {
                if (await frmSpiritNotes.ShowDialogSafeAsync(_objSpirit.CharacterObject).ConfigureAwait(false) != DialogResult.OK)
                    return;
                _objSpirit.Notes = frmSpiritNotes.MyForm.Notes;
            }

            string strTooltip = await LanguageManager.GetStringAsync(_objSpirit.EntityType == SpiritType.Spirit ? "Tip_Spirit_EditNotes" : "Tip_Sprite_EditNotes").ConfigureAwait(false);

            if (!string.IsNullOrEmpty(_objSpirit.Notes))
                strTooltip += Environment.NewLine + Environment.NewLine + _objSpirit.Notes;
            await cmdNotes.SetToolTipTextAsync(strTooltip.WordWrap()).ConfigureAwait(false);

            ContactDetailChanged?.Invoke(this, e);
        }

        #endregion Control Events

        #region Properties

        /// <summary>
        /// Spirit object this is linked to.
        /// </summary>
        public Spirit SpiritObject => _objSpirit;

        #endregion Properties

        #region Methods

        // Rebuild the list of Spirits/Sprites based on the character's selected Tradition/Stream.
        public async void RebuildSpiritListOnTraditionChange(object sender, PropertyChangedEventArgs e)
        {
            if (e?.PropertyName == nameof(Character.MagicTradition))
            {
                await RebuildSpiritList(_objSpirit.CharacterObject.MagicTradition).ConfigureAwait(false);
            }
        }

        // Rebuild the list of Spirits/Sprites based on the character's selected Tradition/Stream.
        public async ValueTask RebuildSpiritList(Tradition objTradition, CancellationToken token = default)
        {
            if (objTradition == null)
                return;
            string strCurrentValue = await cboSpiritName.DoThreadSafeFuncAsync(x => x.SelectedValue?.ToString(), token: token).ConfigureAwait(false) ?? _objSpirit.Name;

            XPathNavigator objXmlDocument = await _objSpirit.CharacterObject.LoadDataXPathAsync(_objSpirit.EntityType == SpiritType.Spirit
                ? "traditions.xml"
                : "streams.xml", token: token).ConfigureAwait(false);

            using (new FetchSafelyFromPool<HashSet<string>>(Utils.StringHashSetPool,
                                                            out HashSet<string> setLimitCategories))
            {
                foreach (Improvement objImprovement in await ImprovementManager.GetCachedImprovementListForValueOfAsync(
                             _objSpirit.CharacterObject, Improvement.ImprovementType.LimitSpiritCategory, token: token).ConfigureAwait(false))
                {
                    setLimitCategories.Add(objImprovement.ImprovedName);
                }

                using (new FetchSafelyFromPool<List<ListItem>>(Utils.ListItemListPool, out List<ListItem> lstCritters))
                {
                    if (objTradition.IsCustomTradition)
                    {
                        string strSpiritCombat = objTradition.SpiritCombat;
                        string strSpiritDetection = objTradition.SpiritDetection;
                        string strSpiritHealth = objTradition.SpiritHealth;
                        string strSpiritIllusion = objTradition.SpiritIllusion;
                        string strSpiritManipulation = objTradition.SpiritManipulation;

                        if ((setLimitCategories.Count == 0 || setLimitCategories.Contains(strSpiritCombat))
                            && !string.IsNullOrWhiteSpace(strSpiritCombat))
                        {
                            XPathNavigator objXmlCritterNode
                                = objXmlDocument.SelectSingleNode(
                                    "/chummer/spirits/spirit[name = " + strSpiritCombat.CleanXPath() + ']');
                            string strTranslatedName = objXmlCritterNode != null
                                ? (await objXmlCritterNode.SelectSingleNodeAndCacheExpressionAsync("translate", token: token).ConfigureAwait(false))?.Value
                                  ?? strSpiritCombat
                                : strSpiritCombat;
                            lstCritters.Add(new ListItem(strSpiritCombat, strTranslatedName));
                        }

                        if ((setLimitCategories.Count == 0 || setLimitCategories.Contains(strSpiritDetection))
                            && !string.IsNullOrWhiteSpace(strSpiritDetection))
                        {
                            XPathNavigator objXmlCritterNode
                                = objXmlDocument.SelectSingleNode(
                                    "/chummer/spirits/spirit[name = " + strSpiritDetection.CleanXPath() + ']');
                            string strTranslatedName = objXmlCritterNode != null
                                ? (await objXmlCritterNode.SelectSingleNodeAndCacheExpressionAsync("translate", token: token).ConfigureAwait(false))?.Value
                                  ?? strSpiritDetection
                                : strSpiritDetection;
                            lstCritters.Add(new ListItem(strSpiritDetection, strTranslatedName));
                        }

                        if ((setLimitCategories.Count == 0 || setLimitCategories.Contains(strSpiritHealth))
                            && !string.IsNullOrWhiteSpace(strSpiritHealth))
                        {
                            XPathNavigator objXmlCritterNode
                                = objXmlDocument.SelectSingleNode(
                                    "/chummer/spirits/spirit[name = " + strSpiritHealth.CleanXPath() + ']');
                            string strTranslatedName = objXmlCritterNode != null
                                ? (await objXmlCritterNode.SelectSingleNodeAndCacheExpressionAsync("translate", token: token).ConfigureAwait(false))?.Value
                                  ?? strSpiritHealth
                                : strSpiritHealth;
                            lstCritters.Add(new ListItem(strSpiritHealth, strTranslatedName));
                        }

                        if ((setLimitCategories.Count == 0 || setLimitCategories.Contains(strSpiritIllusion))
                            && !string.IsNullOrWhiteSpace(strSpiritIllusion))
                        {
                            XPathNavigator objXmlCritterNode
                                = objXmlDocument.SelectSingleNode(
                                    "/chummer/spirits/spirit[name = " + strSpiritIllusion.CleanXPath() + ']');
                            string strTranslatedName = objXmlCritterNode != null
                                ? (await objXmlCritterNode.SelectSingleNodeAndCacheExpressionAsync("translate", token: token).ConfigureAwait(false))?.Value
                                  ?? strSpiritIllusion
                                : strSpiritIllusion;
                            lstCritters.Add(new ListItem(strSpiritIllusion, strTranslatedName));
                        }

                        if ((setLimitCategories.Count == 0 || setLimitCategories.Contains(strSpiritManipulation))
                            && !string.IsNullOrWhiteSpace(strSpiritManipulation))
                        {
                            XPathNavigator objXmlCritterNode
                                = objXmlDocument.SelectSingleNode(
                                    "/chummer/spirits/spirit[name = " + strSpiritManipulation.CleanXPath() + ']');
                            string strTranslatedName = objXmlCritterNode != null
                                ? (await objXmlCritterNode.SelectSingleNodeAndCacheExpressionAsync("translate", token: token).ConfigureAwait(false))?.Value
                                  ?? strSpiritManipulation
                                : strSpiritManipulation;
                            lstCritters.Add(new ListItem(strSpiritManipulation, strTranslatedName));
                        }
                    }
                    else
                    {
                        XPathNavigator objDataNode = await objTradition.GetNodeXPathAsync(token: token).ConfigureAwait(false);
                        if (objDataNode != null && await objDataNode.SelectSingleNodeAndCacheExpressionAsync("spirits/spirit[. = \"All\"]", token).ConfigureAwait(false) != null)
                        {
                            if (setLimitCategories.Count == 0)
                            {
                                foreach (XPathNavigator objXmlCritterNode in await objXmlDocument.SelectAndCacheExpressionAsync(
                                             "/chummer/spirits/spirit", token: token).ConfigureAwait(false))
                                {
                                    string strSpiritName = (await objXmlCritterNode.SelectSingleNodeAndCacheExpressionAsync("name", token: token).ConfigureAwait(false))
                                                                            ?.Value;
                                    lstCritters.Add(new ListItem(strSpiritName,
                                                                 (await objXmlCritterNode
                                                                        .SelectSingleNodeAndCacheExpressionAsync("translate", token: token).ConfigureAwait(false))
                                                                     ?.Value
                                                                 ?? strSpiritName));
                                }
                            }
                            else
                            {
                                foreach (string strSpiritName in setLimitCategories)
                                {
                                    XPathNavigator objXmlCritterNode
                                        = objXmlDocument.SelectSingleNode(
                                            "/chummer/spirits/spirit[name = " + strSpiritName.CleanXPath() + ']');
                                    string strTranslatedName = objXmlCritterNode != null
                                        ? (await objXmlCritterNode.SelectSingleNodeAndCacheExpressionAsync("translate", token: token).ConfigureAwait(false))?.Value
                                          ?? strSpiritName
                                        : strSpiritName;
                                    lstCritters.Add(new ListItem(strSpiritName, strTranslatedName));
                                }
                            }
                        }
                        else
                        {
                            XPathNavigator objTraditionNode = await objTradition.GetNodeXPathAsync(token: token).ConfigureAwait(false);
                            XPathNodeIterator xmlSpiritList = objTraditionNode != null ? await objTraditionNode.SelectAndCacheExpressionAsync("spirits/*", token).ConfigureAwait(false) : null;
                            if (xmlSpiritList != null)
                            {
                                foreach (XPathNavigator objXmlSpirit in xmlSpiritList)
                                {
                                    string strSpiritName = objXmlSpirit.Value;
                                    if (setLimitCategories.Count == 0 || setLimitCategories.Contains(strSpiritName))
                                    {
                                        XPathNavigator objXmlCritterNode
                                            = objXmlDocument.SelectSingleNode(
                                                "/chummer/spirits/spirit[name = " + strSpiritName.CleanXPath()
                                                + ']');
                                        string strTranslatedName = objXmlCritterNode != null
                                            ? (await objXmlCritterNode.SelectSingleNodeAndCacheExpressionAsync("translate", token: token).ConfigureAwait(false))?.Value
                                              ?? strSpiritName
                                            : strSpiritName;
                                        lstCritters.Add(new ListItem(strSpiritName, strTranslatedName));
                                    }
                                }
                            }
                        }
                    }

                    // Add any additional Spirits and Sprites the character has Access to through improvements.

                    if (_objSpirit.CharacterObject.MAGEnabled)
                    {
                        foreach (Improvement objImprovement in await ImprovementManager.GetCachedImprovementListForValueOfAsync(
                                     _objSpirit.CharacterObject, Improvement.ImprovementType.AddSpirit, token: token).ConfigureAwait(false))
                        {
                            string strImprovedName = objImprovement.ImprovedName;
                            if (!string.IsNullOrEmpty(strImprovedName))
                            {
                                lstCritters.Add(new ListItem(strImprovedName,
                                                             objXmlDocument
                                                                 .SelectSingleNode(
                                                                     "/chummer/spirits/spirit[name = "
                                                                     + strImprovedName.CleanXPath() + "]/translate")
                                                                 ?.Value
                                                             ?? strImprovedName));
                            }
                        }
                    }

                    if (_objSpirit.CharacterObject.RESEnabled)
                    {
                        foreach (Improvement objImprovement in await ImprovementManager.GetCachedImprovementListForValueOfAsync(
                                     _objSpirit.CharacterObject, Improvement.ImprovementType.AddSprite, token: token).ConfigureAwait(false))
                        {
                            string strImprovedName = objImprovement.ImprovedName;
                            if (!string.IsNullOrEmpty(strImprovedName))
                            {
                                lstCritters.Add(new ListItem(strImprovedName,
                                                             objXmlDocument
                                                                 .SelectSingleNode(
                                                                     "/chummer/spirits/spirit[name = "
                                                                     + strImprovedName.CleanXPath() + "]/translate")
                                                                 ?.Value
                                                             ?? strImprovedName));
                            }
                        }
                    }

                    await cboSpiritName.PopulateWithListItemsAsync(lstCritters, token: token).ConfigureAwait(false);
                    // Set the control back to its original value.
                    await cboSpiritName.DoThreadSafeAsync(x => x.SelectedValue = strCurrentValue, token: token).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Create a Critter, put them into Career Mode, link them, and open the newly-created Critter.
        /// </summary>
        /// <param name="strCritterName">Name of the Critter's Metatype.</param>
        /// <param name="intForce">Critter's Force.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        private async ValueTask CreateCritter(string strCritterName, int intForce, CancellationToken token = default)
        {
            // Code from frmMetatype.
            XmlDocument objXmlDocument = await _objSpirit.CharacterObject.LoadDataAsync("critters.xml", token: token).ConfigureAwait(false);

            XmlNode objXmlMetatype = objXmlDocument.TryGetNodeByNameOrId("/chummer/metatypes/metatype", strCritterName);

            // If the Critter could not be found, show an error and get out of here.
            if (objXmlMetatype == null)
            {
                Program.ShowScrollableMessageBox(
                    string.Format(GlobalSettings.CultureInfo,
                                  await LanguageManager.GetStringAsync("Message_UnknownCritterType", token: token).ConfigureAwait(false),
                                  strCritterName),
                    await LanguageManager.GetStringAsync("MessageTitle_SelectCritterType", token: token).ConfigureAwait(false),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            CursorWait objCursorWait = await CursorWait.NewAsync(ParentForm, token: token).ConfigureAwait(false);
            try
            {
                // The Critter should use the same settings file as the character.
                Character objCharacter = new Character();
                await objCharacter.SetSettingsKeyAsync(await _objSpirit.CharacterObject.GetSettingsKeyAsync(token).ConfigureAwait(false),
                                                       token).ConfigureAwait(false);
                // Override the defaults for the setting.
                objCharacter.IgnoreRules = true;
                objCharacter.IsCritter = true;
                objCharacter.Alias = strCritterName;
                await objCharacter.SetCreatedAsync(true, token: token).ConfigureAwait(false);
                try
                {
                    string strCritterCharacterName = await txtCritterName.DoThreadSafeFuncAsync(x => x.Text, token: token).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(strCritterCharacterName))
                        objCharacter.Name = strCritterCharacterName;

                    string strSpace = await LanguageManager.GetStringAsync("String_Space", token: token).ConfigureAwait(false);
                    string strFileName = string.Empty;
                    string strFilter = await LanguageManager.GetStringAsync("DialogFilter_Chum5", token: token).ConfigureAwait(false) + '|' +
                        await LanguageManager.GetStringAsync("DialogFilter_Chum5lz", token: token).ConfigureAwait(false) + '|' +
                        await LanguageManager.GetStringAsync("DialogFilter_All", token: token).ConfigureAwait(false);
                    string strInputFileName = strCritterName + strSpace + '('
                                              + string.Format(
                                                  GlobalSettings.CultureInfo,
                                                  await LanguageManager
                                                        .GetStringAsync("Label_RatingFormat", token: token)
                                                        .ConfigureAwait(false), await LanguageManager
                                                      .GetStringAsync(_objSpirit.RatingLabel, token: token)
                                                      .ConfigureAwait(false)) + strSpace
                                              + _objSpirit.Force.ToString(GlobalSettings.InvariantCultureInfo);
                    DialogResult eResult = await this.DoThreadSafeFuncAsync(x =>
                    {
                        using (SaveFileDialog dlgSaveFile = new SaveFileDialog())
                        {
                            dlgSaveFile.Filter = strFilter;
                            dlgSaveFile.FileName = strInputFileName;
                            DialogResult eReturn = dlgSaveFile.ShowDialog(x);
                            strFileName = dlgSaveFile.FileName;
                            return eReturn;
                        }
                    }, token: token).ConfigureAwait(false);

                    if (eResult != DialogResult.OK)
                        return;

                    if (!strFileName.EndsWith(".chum5", StringComparison.OrdinalIgnoreCase)
                        && !strFileName.EndsWith(".chum5lz", StringComparison.OrdinalIgnoreCase))
                        strFileName += ".chum5";
                    objCharacter.FileName = strFileName;

                    objCharacter.Create(objXmlMetatype["category"]?.InnerText, objXmlMetatype["id"]?.InnerText,
                                        string.Empty, objXmlMetatype, intForce, token: token);
                    objCharacter.MetatypeBP = 0;
                    using (ThreadSafeForm<LoadingBar> frmLoadingBar = await Program.CreateAndShowProgressBarAsync(token: token).ConfigureAwait(false))
                    {
                        await frmLoadingBar.MyForm.PerformStepAsync(objCharacter.CharacterName,
                                                                    LoadingBar.ProgressBarTextPatterns.Saving, token).ConfigureAwait(false);
                        if (!await objCharacter.SaveAsync(token: token).ConfigureAwait(false))
                            return;
                    }

                    // Link the newly-created Critter to the Spirit.
                    string strText = await LanguageManager.GetStringAsync(
                        _objSpirit.EntityType == SpiritType.Spirit ? "Tip_Spirit_OpenFile" : "Tip_Sprite_OpenFile", token: token).ConfigureAwait(false);
                    await cmdLink.SetToolTipTextAsync(strText, token).ConfigureAwait(false);
                    ContactDetailChanged?.Invoke(this, EventArgs.Empty);

                    await Program.OpenCharacter(objCharacter, token: token).ConfigureAwait(false);
                }
                finally
                {
                    await objCharacter
                          .DisposeAsync().ConfigureAwait(false); // Fine here because Dispose()/DisposeAsync() code is skipped if the character is open in a form
                }
            }
            finally
            {
                await objCursorWait.DisposeAsync().ConfigureAwait(false);
            }
        }

        #endregion Methods
    }
}
