using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class GoogleSpreadSheetHelper
{
    private static string GoogleSpreadSheetURLPrefix = "https://docs.google.com/spreadsheets/d";

    public static string GetSheetUrl(string sheetID)
    {
        return $"{GoogleSpreadSheetURLPrefix}/{sheetID}/edit";
    }
    public static string GetSheetUrl(string sheetID, string gid)
    {
        return $"{GoogleSpreadSheetURLPrefix}/{sheetID}/edit#gid={gid}";
    }
    public static string GetSheetUrl(string sheetID, string gid, string range)
    {
        return $"{GoogleSpreadSheetURLPrefix}/{sheetID}/edit#gid={gid}&range={range}";
    }

    public static string ConvertR1C1ToA1(int row, int column)
    {
        var range = "A1";
        if (row < 1 || column < 1)
        {
            return range;
        }

        var sb = new StringBuilder();
        double unparsedColumn = column;
        // 26characters(A-Z)
        var columnCharacterCycle = 26;

        var digitIndex = 1;

        while (unparsedColumn > 0)
        {
            var baseValue = Math.Pow(columnCharacterCycle + 1, digitIndex);
            var beforeBaseValue = digitIndex == 1 ? 1 : Math.Pow(columnCharacterCycle + 1, digitIndex - 1);
            var rawRange = unparsedColumn < baseValue ? unparsedColumn : unparsedColumn % baseValue;

            var value = rawRange / beforeBaseValue;

            char symbol = value == 0 ? 'A' : (char)('A' + value -1);
            sb.Insert(0, symbol);

            unparsedColumn -= rawRange;
            digitIndex += 1;
        }

        range = $"{sb}{row}";
        return range;
    }
}
