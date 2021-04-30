using NUnit.Framework;
using System;
using UnityEngine;

public class GoogleSpreadSheetHelperTest
{
    public string sheetID = "1Go5YXCeBYCY7c2-8r7M5Hn3EF0tbWHYzVcSF1yd7onA";

    [Test]
    public void GetSheetUrlBySheetId()
    {
        var expected = $"https://docs.google.com/spreadsheets/d/{sheetID}/edit";
        var actual = GoogleSpreadSheetHelper.GetSheetUrl(sheetID);

        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void GetSheetUrlBySheetIdAndGID()
    {
        var expected = $"https://docs.google.com/spreadsheets/d/{sheetID}/edit#gid=0";
        var actual = GoogleSpreadSheetHelper.GetSheetUrl(sheetID, 0.ToString());

        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void GetSheetUrlBySheetIdAndGIDAndRange()
    {
        var expected = $"https://docs.google.com/spreadsheets/d/{sheetID}/edit#gid=0&range=A1";
        var actual = GoogleSpreadSheetHelper.GetSheetUrl(sheetID, 0.ToString(), "A1");

        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void ConvertR1C1ToRangeA1()
    {
        var actual = GoogleSpreadSheetHelper.ConvertR1C1ToA1(1, 1);
        var expected = "A1";

        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void ConvertR1C1ToRangeAA1()
    {
        var actual = GoogleSpreadSheetHelper.ConvertR1C1ToA1(1, 27);
        var expected = "AA1";

        Assert.AreEqual(expected, actual);
    }
    [Test]
    public void ConvertR1C1ToRangeAZ1()
    {
        var cycle = 27;
        var actual = GoogleSpreadSheetHelper.ConvertR1C1ToA1(1, cycle + (cycle -1));
        var expected = "AZ1";

        Assert.AreEqual(expected, actual);
    }
    [Test]
    public void ConvertR1C1ToRangeAAA1()
    {
        var actual = GoogleSpreadSheetHelper.ConvertR1C1ToA1(1, 27 + (int)Math.Pow(27, 2));
        var expected = "AAA1";

        Assert.AreEqual(expected, actual);
    }
    [Test]
    public void ConvertR1C1ToRangeAZZ1()
    {
        var cycle = 27;
        var actual = GoogleSpreadSheetHelper.ConvertR1C1ToA1(1, ((int) Math.Pow(cycle, 2)) +  ((int) Math.Pow(cycle, 2) - cycle) + (cycle -1));
        var expected = "AZZ1";

        Assert.AreEqual(expected, actual);
    }
    [Test]
    public void ConvertR1C1ToRangeAZY1()
    {
        var cycle = 27;
        var actual = GoogleSpreadSheetHelper.ConvertR1C1ToA1(1, ((int) Math.Pow(cycle, 2)) +  ((int) Math.Pow(cycle, 2) - cycle) + (cycle -2));
        var expected = "AZY1";

        Assert.AreEqual(expected, actual);
    }
}
