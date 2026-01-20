namespace HEAppE.BusinessLogicTier.Configuration;

/// <summary>
/// Role assignments configuration
/// </summary>
public sealed class RoleAssignmentConfiguration
{
   /// <summary>
   /// Administrators role assignments
   /// </summary>
   public static string[] Administrators { get; set; } = ["admin"];
   /// <summary>
   /// Maintainers role assignments
   /// </summary>
   public static string[] Maintainers { get; set; }
   /// <summary>
   /// Managers role assignments
   /// </summary>
   public static string[] Managers { get; set; }
   /// <summary>
   /// Submitters role assignments
   /// </summary>
   public static string[] Submitters { get; set; }
   /// <summary>
   /// Group reporters role assignments
   /// </summary>
   public static string[] GroupReporters { get; set; }
   /// <summary>
   /// Reporters role assignments
   /// </summary>
   public static string[] Reporters { get; set; }
   /// <summary>
   /// Management admins role assignments
   /// </summary>
   public static string[] ManagementAdmins { get; set; }
}