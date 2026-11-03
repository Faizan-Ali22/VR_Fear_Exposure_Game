using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FearExposure.Level1
{
    /// <summary>
    /// A small checklist board ("find the red / green / blue object").
    /// FindTargetsStep fills the rows and ticks them off.
    /// </summary>
    public class ObjectiveListPanel : MonoBehaviour
    {
        [Serializable]
        public class Row
        {
            public GameObject root;
            public TMP_Text label;
            public Image dot;
            public GameObject checkMark;
        }

        [SerializeField] private TMP_Text headerText;
        [SerializeField] private Row[] rows;
        [SerializeField] private Color pendingTextColor = Color.white;
        [SerializeField] private Color doneTextColor = new Color(0.55f, 1f, 0.7f, 1f);

        public int RowCount { get { return rows != null ? rows.Length : 0; } }

        public void SetHeader(string text)
        {
            if (headerText != null) headerText.text = text;
        }

        public void Configure(int index, string text, Color dotColor)
        {
            Row row = Get(index);
            if (row == null) return;
            if (row.root != null) row.root.SetActive(true);
            if (row.label != null)
            {
                row.label.text = text;
                row.label.color = pendingTextColor;
            }
            if (row.dot != null) row.dot.color = dotColor;
            if (row.checkMark != null) row.checkMark.SetActive(false);
        }

        public void MarkDone(int index)
        {
            Row row = Get(index);
            if (row == null) return;
            if (row.checkMark != null) row.checkMark.SetActive(true);
            if (row.label != null) row.label.color = doneTextColor;
        }

        public void HideAllRows()
        {
            if (rows == null) return;
            for (int i = 0; i < rows.Length; i++)
                if (rows[i] != null && rows[i].root != null) rows[i].root.SetActive(false);
        }

        private Row Get(int index)
        {
            if (rows == null || index < 0 || index >= rows.Length) return null;
            return rows[index];
        }

        public void Validate(ValidationReport report, int rowsNeeded)
        {
            if (rows == null || rows.Length < rowsNeeded)
                report.Error("Needs at least " + rowsNeeded + " Rows (has " + (rows == null ? 0 : rows.Length) + ").", this);
            else
                for (int i = 0; i < rowsNeeded; i++)
                {
                    if (rows[i] == null) { report.Error("Rows[" + i + "] is empty.", this); continue; }
                    if (rows[i].root == null) report.Error("Rows[" + i + "].Root is not assigned.", this);
                    if (rows[i].label == null) report.Error("Rows[" + i + "].Label is not assigned.", this);
                    if (rows[i].dot == null) report.Error("Rows[" + i + "].Dot is not assigned.", this);
                    if (rows[i].checkMark == null) report.Error("Rows[" + i + "].Check Mark is not assigned.", this);
                }
        }
    }
}
