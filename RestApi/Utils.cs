﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HEAppE.RestApi
{
    /// <summary>
    /// Utils
    /// </summary>
    internal static  class Utils
    {
        #region Methods
        /// <summary>
        /// Remove specific character from begining or end of string
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="character">Character</param>
        /// <returns></returns>

        internal static string RemoveCharacterFromBeginAndEnd(string value, char character)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            else
            {
                string tempValue = value;
                if (tempValue.ElementAt(0) == character)
                {
                    tempValue = tempValue.Substring(1);
                }
                int lastCharPosition = tempValue.Length - 1;
                if (tempValue.ElementAt(lastCharPosition) == character)
                {
                    tempValue.Substring(0, lastCharPosition);
                }
                return tempValue;
            }
        }
        #endregion
    }
}
