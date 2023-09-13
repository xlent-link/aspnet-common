namespace XlentLink.Commons.Startup;

public class ComponentSettings
{
    /// <summary>
    /// Component name
    /// </summary>
    public string ApplicationName { get; set; } = null!;

    /// <summary>
    /// Deployment environment, like "test" or "prod". (mandatory)
    /// </summary>
    public string Environment { get; set; } = null!;

    /// <summary>
    /// Settings for Azure Key Vault (optional)
    /// </summary>
    public KeyVaultSettings? KeyVault { get; set; }
}

public class KeyVaultSettings
{
    /// <summary>
    /// Uri to the key vault
    /// </summary>
    public string? Uri { get; set; }

    /// <summary>
    /// For local development
    /// </summary>
    public bool UseInteractiveBrowserCredential { get; set; }
}
