using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using log4net;

namespace HEAppE.Utils;

public static class ResourceAccountingUtils
{
    private static readonly DataTable _calculator = new DataTable();
    private static readonly char[] _operators = "+-*/%()".ToCharArray();

    public static void ComputeAccounting(SubmittedTaskInfo dbTaskInfo, SubmittedTaskInfo submittedTaskInfo, ILog logger)
    {
        if (dbTaskInfo == null || submittedTaskInfo == null)
        {
            logger?.Error("Cannot compute accounting: dbTaskInfo or submittedTaskInfo is null.");
            return;
        }

        logger?.Info($"Choosing accounting for SubmittedTaskInfo: {dbTaskInfo.Id}, StartTime: {submittedTaskInfo.StartTime}, EndTime: {submittedTaskInfo.EndTime}");

        var accounting = dbTaskInfo.NodeType
            ?.ClusterNodeTypeAggregation
            ?.ClusterNodeTypeAggregationAccountings
            ?.Where(x => x.Accounting is { IsDeleted: false } && x.Accounting.IsValid(submittedTaskInfo.StartTime, submittedTaskInfo.EndTime))
            .OrderByDescending(x => x.Accounting.ValidityFrom)
            .ThenByDescending(x => x.Accounting.Id)
            .Select(x => x.Accounting)
            .FirstOrDefault();

        if (accounting == null)
        {
            logger?.Info($"Accounting not found for SubmittedTaskInfo: {dbTaskInfo.Id}");
            return;
        }

        logger?.Info($"Accounting {accounting.Id} found for SubmittedTaskInfo: {submittedTaskInfo.Id}");

        if (submittedTaskInfo.ParsedParameters == null || submittedTaskInfo.ParsedParameters.Count == 0)
        {
            submittedTaskInfo.ParsedParameters = submittedTaskInfo.AllParameters
                ?.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split('=', StringSplitOptions.RemoveEmptyEntries))
                .Where(x => x.Length == 2)
                .GroupBy(x => x[0])
                .ToDictionary(g => g.Key, g => g.First()[1]) ?? new Dictionary<string, string>();
        }

        var resourceAccountingValue = CalculateAllocatedResources(accounting.Formula, submittedTaskInfo.ParsedParameters, logger);

        dbTaskInfo.ResourceConsumed ??= new ResourceConsumed();
        dbTaskInfo.ResourceConsumed.Value = resourceAccountingValue;
        dbTaskInfo.ResourceConsumed.LastUpdatedAt = DateTime.UtcNow;
        dbTaskInfo.ResourceConsumed.Accounting = accounting;
    }

    private static double CalculateAllocatedResources(string accountingFormula, Dictionary<string, string> parsedParameters, ILog logger)
    {
        if (string.IsNullOrWhiteSpace(accountingFormula)) return 0;

        accountingFormula = accountingFormula.Replace(" ", string.Empty);
        string originalFormula = accountingFormula;

        var formulaProperties = accountingFormula.Split(_operators, StringSplitOptions.RemoveEmptyEntries);

        var relevantParams = parsedParameters
            .Where(w => formulaProperties.Contains(w.Key))
            .OrderByDescending(w => w.Key.Length);

        var paramLogs = new List<string>();

        foreach (var param in relevantParams)
        {
            try
            {
                double value = 0;
                string normalizedValue = param.Value.Replace(',', '.');

                if (double.TryParse(normalizedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double dVal))
                {
                    value = dVal;
                }
                else if (TimeSpan.TryParse(param.Value, CultureInfo.InvariantCulture, out TimeSpan time))
                {
                    value = time.TotalHours;
                }

                paramLogs.Add($"{param.Key}='{param.Value}'->{value}");

                accountingFormula = accountingFormula.Replace(param.Key, value.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                logger?.Error($"Error processing parameter {param.Key}: {ex.Message}");
            }
        }

        try
        {
            var resultObj = _calculator.Compute(accountingFormula, null);
            double finalResult = Convert.ToDouble(resultObj, CultureInfo.InvariantCulture);

            logger?.Info($"Allocated Resources | OrigFormula: [{originalFormula}] | Params: [{string.Join(", ", paramLogs)}] | ParsedFormula: [{accountingFormula}] | Result: {finalResult}");

            return finalResult;
        }
        catch (Exception ex)
        {
            logger?.Error($"Error computing final formula [{accountingFormula}]: {ex.Message}");
        }

        return 0;
    }
}