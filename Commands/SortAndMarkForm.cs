// Commands/SortAndMarkForm.cs
using System;
using System.Collections.Generic;
using System.Linq;

using WF = System.Windows.Forms;

namespace RebarTools.Commands
{
    internal class SortAndMarkForm : WF.Form
    {
        private WF.ComboBox cb1, cb2, cb3;
        private WF.TextBox tbStart;
        private WF.Label lblWarn;
        private WF.Button btnOk, btnCancel;

        public SortKey? PrimaryKey { get; private set; }
        public SortKey? SecondaryKey { get; private set; }
        public SortKey? TertiaryKey { get; private set; }
        public int StartNumber { get; private set; }

        public SortAndMarkForm()
        {
            Text = "Sort & Mark Hosted Rebars";
            FormBorderStyle = WF.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = WF.FormStartPosition.CenterParent;
            Width = 430;
            Height = 270;

            var lbl1 = new WF.Label { Left = 20, Top = 20, Width = 130, Text = "Primary sort:" };
            var lbl2 = new WF.Label { Left = 20, Top = 55, Width = 130, Text = "Secondary sort:" };
            var lbl3 = new WF.Label { Left = 20, Top = 90, Width = 130, Text = "Tertiary sort:" };
            var lblStart = new WF.Label { Left = 20, Top = 130, Width = 130, Text = "Start number:" };

            cb1 = new WF.ComboBox { Left = 160, Top = 16, Width = 230, DropDownStyle = WF.ComboBoxStyle.DropDownList };
            cb2 = new WF.ComboBox { Left = 160, Top = 51, Width = 230, DropDownStyle = WF.ComboBoxStyle.DropDownList };
            cb3 = new WF.ComboBox { Left = 160, Top = 86, Width = 230, DropDownStyle = WF.ComboBoxStyle.DropDownList };

            tbStart = new WF.TextBox { Left = 160, Top = 126, Width = 90, Text = "1" };

            lblWarn = new WF.Label { Left = 20, Top = 158, Width = 370, Height = 18, Text = "" };

            btnOk = new WF.Button { Left = 235, Top = 190, Width = 75, Text = "OK", DialogResult = WF.DialogResult.OK };
            btnCancel = new WF.Button { Left = 315, Top = 190, Width = 75, Text = "Cancel", DialogResult = WF.DialogResult.Cancel };

            Controls.AddRange(new WF.Control[]
            {
                lbl1, lbl2, lbl3, lblStart,
                cb1, cb2, cb3,
                tbStart, lblWarn,
                btnOk, btnCancel
            });

            // Display names (what user sees) -> enum values (what code uses)
            var items = new List<KeyValuePair<string, SortKey?>>();
            items.Add(new KeyValuePair<string, SortKey?>("None", null));
            items.Add(new KeyValuePair<string, SortKey?>("Bar Length", SortKey.BarLength));
            items.Add(new KeyValuePair<string, SortKey?>("Diameter", SortKey.Diameter));
            items.Add(new KeyValuePair<string, SortKey?>("Shape", SortKey.Shape));

            BindCombo(cb1, items);
            BindCombo(cb2, items);
            BindCombo(cb3, items);

            cb1.SelectedIndex = 1; // default: Bar Length
            cb2.SelectedIndex = 0;
            cb3.SelectedIndex = 0;

            cb1.SelectedIndexChanged += (_, __) => ValidateUnique();
            cb2.SelectedIndexChanged += (_, __) => ValidateUnique();
            cb3.SelectedIndexChanged += (_, __) => ValidateUnique();

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            btnOk.Click += (_, __) =>
            {
                // Prevent duplicates (Diameter twice etc.)
                if (!ValidateUnique())
                {
                    DialogResult = WF.DialogResult.None;
                    return;
                }

                // Validate start number
                if (!int.TryParse(tbStart.Text.Trim(), out int start) || start < 0)
                {
                    lblWarn.Text = "Start number must be a non-negative integer.";
                    DialogResult = WF.DialogResult.None;
                    return;
                }

                StartNumber = start;
                PrimaryKey = (SortKey?)cb1.SelectedValue;
                SecondaryKey = (SortKey?)cb2.SelectedValue;
                TertiaryKey = (SortKey?)cb3.SelectedValue;
            };
        }

        private static void BindCombo(WF.ComboBox cb, List<KeyValuePair<string, SortKey?>> items)
        {
            cb.DataSource = items.ToList();
            cb.DisplayMember = "Key";
            cb.ValueMember = "Value";
        }

        private bool ValidateUnique()
        {
            lblWarn.Text = "";

            var chosen = new List<SortKey?>(new[]
            {
                (SortKey?)cb1.SelectedValue,
                (SortKey?)cb2.SelectedValue,
                (SortKey?)cb3.SelectedValue
            });

            var nonNull = chosen.Where(x => x.HasValue).Select(x => x.Value).ToList();
            bool hasDup = nonNull.Count != nonNull.Distinct().Count();

            if (hasDup)
            {
                lblWarn.Text = "Each sort parameter can be used only once.";
                return false;
            }

            return true;
        }
    }
}
