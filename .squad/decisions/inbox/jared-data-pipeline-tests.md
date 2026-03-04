# Bug Fix: MySqlDumpParser VALUES Position Calculation

**By:** Jared (Tester)
**Date:** 2026-03-04
**Affects:** `src/api/WeRace.DataImport/Importers/MySqlDumpParser.cs`

## What

Fixed an off-by-one bug in `ProcessInsertStatement` where the `VALUES` keyword position was computed incorrectly, causing the parser to silently return empty results for every INSERT statement.

## Why

The original code used `match.Index + match.Length - 6` to locate `VALUES` in the matched statement. The regex `\s*` after `VALUES` consumed the trailing space, making the match one character longer than expected. This caused `IndexOf("VALUES", startIndex)` to start searching one position past where `VALUES` actually begins, resulting in a failed lookup and zero parsed rows.

## Fix

Changed the search origin from the end-of-match offset to `match.Groups[1].Index + match.Groups[1].Length` (i.e., after the captured table name), which correctly finds `VALUES` regardless of trailing whitespace.

## Impact

Without this fix, the data import CLI would parse zero rows from any MySQL dump file. The bug was caught by the new `MySqlDumpParserTests` suite.
