using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Chummer;


namespace Chummer.Backend.BuySellIncreaseDecreaseMethods
{
    /// <summary>
    /// This class holds all kinds of different methods that are used to increase/decrease attributes.
    /// </summary>
    public static class ChangeAttributes
    {
        /// <summary>
        /// Increases the powerpoints of an mystic adept in career mode and creates the Karma expense. Only accessable as a house rule.
        /// </summary>
        /// <param name="characterObj"></param>
        public static void IncreasePowerpoint(Character characterObj, frmCareer frmCareer)
        {
            //This could have been passed as a variable, but this makes it more independent from frmCareer
            int intKarmaCost = characterObj.Settings.KarmaMysticAdeptPowerPoint;


            // Create the Karma expense.
            ExpenseLogEntry objExpense = new ExpenseLogEntry(characterObj);
            objExpense.Create(intKarmaCost * -1, LanguageManager.GetString("String_PowerPoint"), ExpenseType.Karma, DateTime.Now);
            characterObj.ExpenseEntries.AddWithSort(objExpense);
            characterObj.Karma -= intKarmaCost;

            ExpenseUndo objUndo = new ExpenseUndo();
            objUndo.CreateKarma(KarmaExpenseType.AddPowerPoint, string.Empty);
            objExpense.Undo = objUndo;

            characterObj.MysticAdeptPowerPoints += 1;

            frmCareer.IsCharacterUpdateRequested = true;
            frmCareer.IsDirty = true;
        }
    }
}
