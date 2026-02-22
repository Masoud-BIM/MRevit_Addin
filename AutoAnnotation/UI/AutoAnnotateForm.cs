using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;
using RebarTools.AutoAnnotation.Models;

using WF = System.Windows.Forms;

namespace RebarTools.AutoAnnotation.UI
{
    internal class AutoAnnotateForm : WF.Form
    {
        private WF.DataGridView _grid;
        private WF.Button _ok;
        private WF.Button _cancel;

        public List<CategoryTagOption> Options { get; private set; }

        public AutoAnnotateForm(List<CategoryTagOption> options)
        {
            Options = options;

            Text = "Auto Annotate View";
            FormBorderStyle = WF.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = WF.FormStartPosition.CenterParent;
            Width = 760;
            Height = 440;

            _grid = new WF.DataGridView
            {
                Left = 12,
                Top = 12,
                Width = 720,
                Height = 340,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = WF.DataGridViewAutoSizeColumnsMode.Fill
            };

            _grid.Columns.Add(new WF.DataGridViewCheckBoxColumn { HeaderText = "Use", Name = "Use", Width = 60 });
            _grid.Columns.Add(new WF.DataGridViewTextBoxColumn { HeaderText = "Category", Name = "Category", ReadOnly = true });
            _grid.Columns.Add(new WF.DataGridViewComboBoxColumn { HeaderText = "Tag Type", Name = "TagType", FlatStyle = WF.FlatStyle.Flat });

            for (int i = 0; i < Options.Count; i++)
            {
                var opt = Options[i];

                int row = _grid.Rows.Add();
                _grid.Rows[row].Cells[0].Value = opt.IsSelected;
                _grid.Rows[row].Cells[1].Value = opt.DisplayName;

                var comboCell = (WF.DataGridViewComboBoxCell)_grid.Rows[row].Cells[2];

                if (opt.AvailableTagTypes == null || opt.AvailableTagTypes.Count == 0)
                {
                    comboCell.DataSource = new List<string> { "(No tag loaded)" };
                    comboCell.Value = "(No tag loaded)";
                    _grid.Rows[row].Cells[0].ReadOnly = true;
                }
                else
                {
                    var items = opt.AvailableTagTypes
                        .Select(s => new TagTypeItem(s.Id, $"{s.FamilyName} : {s.Name}"))
                        .ToList();

                    comboCell.DataSource = items;
                    comboCell.DisplayMember = "Display";
                    comboCell.ValueMember = "Id";
                    comboCell.Value = opt.SelectedTagTypeId;
                }
            }

            _ok = new WF.Button { Text = "OK", Left = 576, Top = 365, Width = 75, DialogResult = WF.DialogResult.OK };
            _cancel = new WF.Button { Text = "Cancel", Left = 657, Top = 365, Width = 75, DialogResult = WF.DialogResult.Cancel };

            Controls.Add(_grid);
            Controls.Add(_ok);
            Controls.Add(_cancel);

            AcceptButton = _ok;
            CancelButton = _cancel;

            _ok.Click += (_, __) =>
            {
                for (int i = 0; i < Options.Count; i++)
                {
                    var opt = Options[i];

                    bool use = _grid.Rows[i].Cells[0].Value is bool b && b;
                    opt.IsSelected = use;

                    if (opt.AvailableTagTypes == null || opt.AvailableTagTypes.Count == 0)
                    {
                        opt.SelectedTagTypeId = ElementId.InvalidElementId;
                        continue;
                    }

                    object val = _grid.Rows[i].Cells[2].Value;
                    opt.SelectedTagTypeId = (val is ElementId id) ? id : ElementId.InvalidElementId;
                }
            };
        }

        private class TagTypeItem
        {
            public ElementId Id { get; }
            public string Display { get; }

            public TagTypeItem(ElementId id, string display)
            {
                Id = id;
                Display = display;
            }
        }
    }
}
