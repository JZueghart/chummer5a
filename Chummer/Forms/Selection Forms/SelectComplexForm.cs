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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.XPath;

namespace Chummer
{
    public partial class SelectComplexForm : Form
    {
        private string _strSelectedComplexForm = string.Empty;

        private bool _blnLoading = true;
        private bool _blnAddAgain;
        private readonly Character _objCharacter;

        private readonly XPathNavigator _xmlBaseComplexFormsNode;
        private readonly XPathNavigator _xmlOptionalComplexFormNode;

        //private bool _blnBiowireEnabled = false;

        #region Control Events

        public SelectComplexForm(Character objCharacter)
        {
            InitializeComponent();
            this.UpdateLightDarkMode();
            this.TranslateWinForm();
            _objCharacter = objCharacter ?? throw new ArgumentNullException(nameof(objCharacter));
            // Load the Complex Form information.
            _xmlBaseComplexFormsNode = _objCharacter.LoadDataXPath("complexforms.xml").SelectSingleNodeAndCacheExpression("/chummer/complexforms");

            _xmlOptionalComplexFormNode = _objCharacter.GetNodeXPath();
            if (_xmlOptionalComplexFormNode == null) return;
            if (_objCharacter.MetavariantGuid != Guid.Empty)
            {
                XPathNavigator xmlMetavariantNode = _xmlOptionalComplexFormNode.TryGetNodeById("metavariants/metavariant", _objCharacter.MetavariantGuid);
                if (xmlMetavariantNode != null)
                    _xmlOptionalComplexFormNode = xmlMetavariantNode;
            }

            _xmlOptionalComplexFormNode = _xmlOptionalComplexFormNode.SelectSingleNodeAndCacheExpression("optionalcomplexforms");
        }

        private async void SelectComplexForm_Load(object sender, EventArgs e)
        {
            _blnLoading = false;
            await BuildComplexFormList().ConfigureAwait(false);
        }

        private async void lstComplexForms_SelectedIndexChanged(object sender, EventArgs e)
        {
            string strSelectedComplexFormId = await lstComplexForms.DoThreadSafeFuncAsync(x => x.SelectedValue?.ToString()).ConfigureAwait(false);
            if (_blnLoading || string.IsNullOrEmpty(strSelectedComplexFormId))
            {
                await tlpRight.DoThreadSafeAsync(x => x.Visible = false).ConfigureAwait(false);
                return;
            }

            // Display the Complex Form information.
            XPathNavigator xmlComplexForm = _xmlBaseComplexFormsNode.TryGetNodeByNameOrId("complexform", strSelectedComplexFormId);
            if (xmlComplexForm == null)
            {
                await tlpRight.DoThreadSafeAsync(x => x.Visible = false).ConfigureAwait(false);
                return;
            }

            await this.DoThreadSafeAsync(x => x.SuspendLayout()).ConfigureAwait(false);
            try
            {
                string strDuration;
                switch ((await xmlComplexForm.SelectSingleNodeAndCacheExpressionAsync("duration").ConfigureAwait(false))?.Value)
                {
                    case "P":
                        strDuration = await LanguageManager.GetStringAsync("String_SpellDurationPermanent").ConfigureAwait(false);
                        break;

                    case "S":
                        strDuration = await LanguageManager.GetStringAsync("String_SpellDurationSustained").ConfigureAwait(false);
                        break;

                    case "Special":
                        strDuration = await LanguageManager.GetStringAsync("String_SpellDurationSpecial").ConfigureAwait(false);
                        break;

                    default:
                        strDuration = await LanguageManager.GetStringAsync("String_SpellDurationInstant").ConfigureAwait(false);
                        break;
                }

                await lblDuration.DoThreadSafeAsync(x => x.Text = strDuration).ConfigureAwait(false);

                string strTarget;
                switch ((await xmlComplexForm.SelectSingleNodeAndCacheExpressionAsync("target").ConfigureAwait(false))?.Value)
                {
                    case "Persona":
                        strTarget = await LanguageManager.GetStringAsync("String_ComplexFormTargetPersona").ConfigureAwait(false);
                        break;

                    case "Device":
                        strTarget = await LanguageManager.GetStringAsync("String_ComplexFormTargetDevice").ConfigureAwait(false);
                        break;

                    case "File":
                        strTarget = await LanguageManager.GetStringAsync("String_ComplexFormTargetFile").ConfigureAwait(false);
                        break;

                    case "Self":
                        strTarget = await LanguageManager.GetStringAsync("String_SpellRangeSelf").ConfigureAwait(false);
                        break;

                    case "Sprite":
                        strTarget = await LanguageManager.GetStringAsync("String_ComplexFormTargetSprite").ConfigureAwait(false);
                        break;

                    case "Host":
                        strTarget = await LanguageManager.GetStringAsync("String_ComplexFormTargetHost").ConfigureAwait(false);
                        break;

                    case "IC":
                        strTarget = await LanguageManager.GetStringAsync("String_ComplexFormTargetIC").ConfigureAwait(false);
                        break;

                    default:
                        strTarget = await LanguageManager.GetStringAsync("String_None").ConfigureAwait(false);
                        break;
                }

                await lblTarget.DoThreadSafeAsync(x => x.Text = strTarget).ConfigureAwait(false);

                string strFv = (await xmlComplexForm.SelectSingleNodeAndCacheExpressionAsync("fv").ConfigureAwait(false))?.Value.Replace('/', '÷').Replace('*', '×')
                               ?? string.Empty;
                if (!GlobalSettings.Language.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
                {
                    strFv = await strFv
                                  .CheapReplaceAsync(
                                      "L", () => LanguageManager.GetStringAsync("String_ComplexFormLevel"))
                                  .CheapReplaceAsync("Overflow damage",
                                                     () => LanguageManager.GetStringAsync("String_SpellOverflowDamage"))
                                  .CheapReplaceAsync("Damage Value",
                                                     () => LanguageManager.GetStringAsync("String_SpellDamageValue"))
                                  .CheapReplaceAsync(
                                      "Toxin DV", () => LanguageManager.GetStringAsync("String_SpellToxinDV"))
                                  .CheapReplaceAsync("Disease DV",
                                                     () => LanguageManager.GetStringAsync("String_SpellDiseaseDV"))
                                  .CheapReplaceAsync("Radiation Power",
                                                     () => LanguageManager.GetStringAsync(
                                                         "String_SpellRadiationPower")).ConfigureAwait(false);
                }

                await lblFV.DoThreadSafeAsync(x => x.Text = strFv).ConfigureAwait(false);

                string strSource = (await xmlComplexForm.SelectSingleNodeAndCacheExpressionAsync("source").ConfigureAwait(false))?.Value ??
                                   await LanguageManager.GetStringAsync("String_Unknown").ConfigureAwait(false);
                string strPage = (await xmlComplexForm.SelectSingleNodeAndCacheExpressionAsync("altpage").ConfigureAwait(false))?.Value ??
                                 (await xmlComplexForm.SelectSingleNodeAndCacheExpressionAsync("page").ConfigureAwait(false))?.Value ??
                                 await LanguageManager.GetStringAsync("String_Unknown").ConfigureAwait(false);
                SourceString objSource = await SourceString.GetSourceStringAsync(
                    strSource, strPage, GlobalSettings.Language,
                    GlobalSettings.CultureInfo, _objCharacter).ConfigureAwait(false);
                string strSourceText = objSource.ToString();
                await lblSource.DoThreadSafeAsync(x => x.Text = strSourceText).ConfigureAwait(false);
                await lblSource.SetToolTipAsync(objSource.LanguageBookTooltip).ConfigureAwait(false);
                await lblTargetLabel.DoThreadSafeAsync(x => x.Visible = !string.IsNullOrEmpty(strTarget)).ConfigureAwait(false);
                await lblDurationLabel.DoThreadSafeAsync(x => x.Visible = !string.IsNullOrEmpty(strDuration)).ConfigureAwait(false);
                await lblSourceLabel.DoThreadSafeAsync(x => x.Visible = !string.IsNullOrEmpty(strSourceText)).ConfigureAwait(false);
                await lblFVLabel.DoThreadSafeAsync(x => x.Visible = !string.IsNullOrEmpty(strFv)).ConfigureAwait(false);
                await tlpRight.DoThreadSafeAsync(x => x.Visible = true).ConfigureAwait(false);
            }
            finally
            {
                await this.DoThreadSafeAsync(x => x.ResumeLayout()).ConfigureAwait(false);
            }
        }

        private void cmdOK_Click(object sender, EventArgs e)
        {
            _blnAddAgain = false;
            AcceptForm();
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void cmdOKAdd_Click(object sender, EventArgs e)
        {
            _blnAddAgain = true;
            AcceptForm();
        }

        private async void txtSearch_TextChanged(object sender, EventArgs e)
        {
            await BuildComplexFormList().ConfigureAwait(false);
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (lstComplexForms.SelectedIndex == -1 && lstComplexForms.Items.Count > 0)
            {
                lstComplexForms.SelectedIndex = 0;
            }
            switch (e.KeyCode)
            {
                case Keys.Down:
                    {
                        int intNewIndex = lstComplexForms.SelectedIndex + 1;
                        if (intNewIndex >= lstComplexForms.Items.Count)
                            intNewIndex = 0;
                        if (lstComplexForms.Items.Count > 0)
                            lstComplexForms.SelectedIndex = intNewIndex;
                        break;
                    }
                case Keys.Up:
                    {
                        int intNewIndex = lstComplexForms.SelectedIndex - 1;
                        if (intNewIndex <= 0)
                            intNewIndex = lstComplexForms.Items.Count - 1;
                        if (lstComplexForms.Items.Count > 0)
                            lstComplexForms.SelectedIndex = intNewIndex;
                        break;
                    }
            }
        }

        private void txtSearch_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
                txtSearch.Select(txtSearch.TextLength, 0);
        }

        #endregion Control Events

        #region Properties

        /// <summary>
        /// Whether or not the user wants to add another item after this one.
        /// </summary>
        public bool AddAgain => _blnAddAgain;

        /// <summary>
        /// Complex Form that was selected in the dialogue.
        /// </summary>
        public string SelectedComplexForm => _strSelectedComplexForm;

        #endregion Properties

        #region Methods

        private async ValueTask BuildComplexFormList(CancellationToken token = default)
        {
            if (_blnLoading)
                return;

            string strFilter = '(' + await _objCharacter.Settings.BookXPathAsync(token: token).ConfigureAwait(false) + ')';
            string strSearch = await txtSearch.DoThreadSafeFuncAsync(x => x.Text, token: token).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(strSearch))
                strFilter += " and " + CommonFunctions.GenerateSearchXPath(strSearch);
            // Populate the Complex Form list.
            using (new FetchSafelyFromPool<List<ListItem>>(Utils.ListItemListPool,
                                                           out List<ListItem> lstComplexFormItems))
            {
                foreach (XPathNavigator xmlComplexForm in _xmlBaseComplexFormsNode.Select(
                             "complexform[" + strFilter + ']'))
                {
                    string strId = (await xmlComplexForm.SelectSingleNodeAndCacheExpressionAsync("id", token: token).ConfigureAwait(false))?.Value;
                    if (string.IsNullOrEmpty(strId))
                        continue;

                    if (!await xmlComplexForm.RequirementsMetAsync(_objCharacter, token: token).ConfigureAwait(false))
                        continue;

                    string strName = (await xmlComplexForm.SelectSingleNodeAndCacheExpressionAsync("name", token: token).ConfigureAwait(false))?.Value
                                     ?? await LanguageManager.GetStringAsync("String_Unknown", token: token).ConfigureAwait(false);
                    // If this is a Sprite with Optional Complex Forms, see if this Complex Form is allowed.
                    if (_xmlOptionalComplexFormNode != null
                        && await _xmlOptionalComplexFormNode.SelectSingleNodeAndCacheExpressionAsync("complexform", token: token).ConfigureAwait(false) != null
                        && _xmlOptionalComplexFormNode.SelectSingleNode("complexform[. = " + strName.CleanXPath() + ']') == null)
                        continue;

                    lstComplexFormItems.Add(
                        new ListItem(strId, (await xmlComplexForm.SelectSingleNodeAndCacheExpressionAsync("translate", token: token).ConfigureAwait(false))?.Value ?? strName));
                }

                lstComplexFormItems.Sort(CompareListItems.CompareNames);
                _blnLoading = true;
                string strOldSelected = await lstComplexForms.DoThreadSafeFuncAsync(x => x.SelectedValue?.ToString(), token: token).ConfigureAwait(false);
                await lstComplexForms.PopulateWithListItemsAsync(lstComplexFormItems, token: token).ConfigureAwait(false);
                _blnLoading = false;
                if (!string.IsNullOrEmpty(strOldSelected))
                    await lstComplexForms.DoThreadSafeAsync(x => x.SelectedValue = strOldSelected, token: token).ConfigureAwait(false);
                else
                    await lstComplexForms.DoThreadSafeAsync(x => x.SelectedIndex = -1, token: token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Accept the selected item and close the form.
        /// </summary>
        private void AcceptForm()
        {
            string strSelectedItem = lstComplexForms.SelectedValue?.ToString();
            if (!string.IsNullOrEmpty(strSelectedItem))
            {
                _strSelectedComplexForm = strSelectedItem;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private async void OpenSourceFromLabel(object sender, EventArgs e)
        {
            await CommonFunctions.OpenPdfFromControl(sender).ConfigureAwait(false);
        }

        #endregion Methods
    }
}
