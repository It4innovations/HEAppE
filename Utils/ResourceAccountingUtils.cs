using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using log4net;

namespace HEAppE.Utils;

public class ResourceAccountingUtils
{
    public static void ComputeAccounting(SubmittedTaskInfo dbTaskInfo, SubmittedTaskInfo submittedTaskInfo, ILog logger)
    {
        logger.Info(
            $"Choosing accounting for SubmittedTaskInfo: {dbTaskInfo.Id}, StartTime: {submittedTaskInfo.StartTime}, EndTime: {submittedTaskInfo.EndTime}");

        var accounting = dbTaskInfo?.NodeType
            ?.ClusterNodeTypeAggregation
            ?.ClusterNodeTypeAggregationAccountings
            ?.Where(x =>
                x.Accounting is { IsDeleted: false } &&
                x.Accounting.IsValid(submittedTaskInfo.StartTime, submittedTaskInfo.EndTime))
            .OrderByDescending(x => x.Accounting.ValidityFrom)
            .ThenByDescending(x => x.Accounting.Id)
            .Select(x => x.Accounting)
            .FirstOrDefault();


        if (accounting == null)
        {
            logger.Error($"Accounting not found for SubmittedTaskInfo: {submittedTaskInfo.Id}");
            return;
        }

        logger.Info($"Accounting {accounting.Id} found for SubmittedTaskInfo: {submittedTaskInfo.Id}");

        if (submittedTaskInfo.ParsedParameters == null || submittedTaskInfo.ParsedParameters.Count == 0)
            submittedTaskInfo.ParsedParameters = submittedTaskInfo.AllParameters
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries))
                .Where(x => x.Length == 2) // Ensure that the split results in exactly two elements
                .ToDictionary(x => x[0], x => x[1]);

        var resourceAccountingValue =
            CalculateAllocatedResources(accounting.Formula, submittedTaskInfo.ParsedParameters, logger);
        if (dbTaskInfo.ResourceConsumed == null)
        {
            dbTaskInfo.ResourceConsumed = new ResourceConsumed
            {
                Value = resourceAccountingValue,
                LastUpdatedAt = DateTime.UtcNow,
                Accounting = accounting
            };
        }
        else
        {
            dbTaskInfo.ResourceConsumed.Value = resourceAccountingValue;
            dbTaskInfo.ResourceConsumed.LastUpdatedAt = DateTime.UtcNow;
            dbTaskInfo.ResourceConsumed.Accounting = accounting;
        }
    }

    private static double CalculateAllocatedResources(string accountingFormula,
        Dictionary<string, string> parsedParameters, ILog logger)
    {
        if (accountingFormula == null || string.IsNullOrEmpty(accountingFormula)) return 0;
        accountingFormula = accountingFormula.Replace(" ", string.Empty);
        logger.Info($"Using accounting formula: {accountingFormula}");
        var accountingFormulaProperties =
            accountingFormula.Split("+-*/%()".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        var filteredParsedParameters = parsedParameters.Where(w => accountingFormulaProperties.Contains(w.Key));

        foreach (var accountingFormulaProperty in filteredParsedParameters)
            try
            {
                double value = 0;
                logger.Info(
                    $"Parsing accounting formula property: {accountingFormulaProperty.Key} with value: {accountingFormulaProperty.Value}");
                if (!double.TryParse(accountingFormulaProperty.Value, out value))
                    if (TimeSpan.TryParse(accountingFormulaProperty.Value, out var time))
                        value = time.TotalHours;
                logger.Info($"Parsed value: {value}");

                accountingFormula =
                    accountingFormula.Replace(accountingFormulaProperty.Key, value.ToString().Replace(',', '.'));
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }

        logger.Info($"Parsed accounting formula: {accountingFormula}");

        try
        {
            var result = new DataTable().Compute(accountingFormula, null);
            return Convert.ToDouble(result);
        }
        catch (Exception ex)
        {
            logger.Error(ex.Message);
        }

        return 0;
    }
}