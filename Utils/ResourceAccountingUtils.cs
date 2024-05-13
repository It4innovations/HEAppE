using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using log4net;
using Microsoft.Extensions.Logging;

namespace HEAppE.Utils;

public class ResourceAccountingUtils
{
    public static double CalculateAllocatedResources(string accountingFormula, Dictionary<string, string> parsedParameters, ILog logger)
    {
        if(accountingFormula == null || string.IsNullOrEmpty(accountingFormula))
        {
            return 0;
        }
        accountingFormula = accountingFormula.Replace(" ", string.Empty);
        var accountingFormulaProperties = accountingFormula.Split("+-*/%()".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        var filteredParsedParameters = parsedParameters.Where(w => accountingFormulaProperties.Contains(w.Key));
    
        foreach (var accountingFormulaProperty in filteredParsedParameters)
        {
            try
            {
                double value = 0;
                if (!double.TryParse(accountingFormulaProperty.Value, out value))
                {
                    if (TimeSpan.TryParse(accountingFormulaProperty.Value, out TimeSpan time))
                    {
                        value = time.TotalHours;
                    }
                }
                    
                accountingFormula = accountingFormula.Replace(accountingFormulaProperty.Key, value.ToString().Replace(',', '.'));
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
        }
    
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