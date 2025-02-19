﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VeloxTimer
{
    public partial class SimpleResult : Form
    {
        public SimpleResult()
        {
            InitializeComponent();
        }

        public void SetTimerData(TimerElement pTimer)
        {
            lblResultTitle.Text = $"Evaluation for category \"{pTimer.CategoryName}\"";

            lblCumulatedToday.Text = pTimer.GetCumulated(CumulateRange.Today).ToString(@"hh\:mm\:ss");
            lblCumulatedYesterday.Text = pTimer.GetCumulated(CumulateRange.Yesterday).ToString(@"hh\:mm\:ss");
            lblCumulatedThisWeek.Text = pTimer.GetCumulated(CumulateRange.ThisWeek).ToString(@"d\:hh\:mm\:ss");
            lblCumulatedLastWeek.Text = pTimer.GetCumulated(CumulateRange.LastWeek).ToString(@"d\:hh\:mm\:ss");
            lblCumulatedThisMonth.Text = pTimer.GetCumulated(CumulateRange.ThisMonth).ToString(@"d\:hh\:mm\:ss");
            lblCumulatedLastMonth.Text = pTimer.GetCumulated(CumulateRange.LastMonth).ToString(@"d\:hh\:mm\:ss");
            lblCumulatedTotal.Text = pTimer.GetCumulated(CumulateRange.Total).ToString(@"d\:hh\:mm\:ss");

            char sign = ' ';

            TimeSpan difDay = pTimer.GetCumulated(CumulateRange.Today).Subtract(pTimer.GetCumulated(CumulateRange.Yesterday));
            if (difDay > TimeSpan.Zero)
            {
                lblDifDay.ForeColor = Color.Green;
                sign = '+';
            }
            else if (difDay < TimeSpan.Zero)
            {
                lblDifDay.ForeColor = Color.Red;
                sign = '-';
            }
            lblDifDay.Text = sign + difDay.ToString(@"hh\:mm\:ss");

            TimeSpan difWeek = pTimer.GetCumulated(CumulateRange.ThisWeek).Subtract(pTimer.GetCumulated(CumulateRange.LastWeek));
            if (difWeek > TimeSpan.Zero)
            {
                lblDifWeek.ForeColor = Color.Green;
                sign = '+';
            }
            else if (difWeek < TimeSpan.Zero)
            {
                lblDifWeek.ForeColor = Color.Red;
                sign = '-';
            }
            lblDifWeek.Text = sign + difWeek.ToString(@"d\:hh\:mm\:ss");

            TimeSpan difMonth = pTimer.GetCumulated(CumulateRange.ThisMonth).Subtract(pTimer.GetCumulated(CumulateRange.LastMonth));
            if (difMonth > TimeSpan.Zero)
            {
                lblDifMonth.ForeColor = Color.Green;
                sign = '+';
            }
            else if (difMonth < TimeSpan.Zero)
            {
                lblDifMonth.ForeColor = Color.Red;
                sign = '-';
            }
            lblDifMonth.Text = sign + difMonth.ToString(@"d\:hh\:mm\:ss");
        }

        private void btnCloseDialog_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
