namespace Carrigan.Emailer;


/// <remarks>
/// Selects how the library should negotiate or require transport security when sending
/// email over the network.
///
/// For production systems, <see cref="SecurityEnum.Tls"/> is strongly recommended
/// unless you have a specific, validated reason to use a different option.
/// Unencrypted or weaker configurations may expose credentials and message data in transit.
/// </remarks>
public enum SecurityEnum
{
    /// <remarks>
    /// Selects how the library should negotiate or require transport security when sending
    /// email over the network.
    ///
    /// For production systems, <see cref="SecurityEnum.Tls"/> is strongly recommended
    /// unless you have a specific, validated reason to use a different option.
    /// Unencrypted or weaker configurations may expose credentials and message data in transit.
    /// </remarks>
    None,
    /// <remarks>
    /// Selects how the library should negotiate or require transport security when sending
    /// email over the network.
    ///
    /// For production systems, <see cref="SecurityEnum.Tls"/> is strongly recommended
    /// unless you have a specific, validated reason to use a different option.
    /// Unencrypted or weaker configurations may expose credentials and message data in transit.
    /// </remarks>
    Ssl,
    /// <remarks>
    /// Selects how the library should negotiate or require transport security when sending
    /// email over the network.
    ///
    /// For production systems, <see cref="SecurityEnum.Tls"/> is strongly recommended
    /// unless you have a specific, validated reason to use a different option.
    /// Unencrypted or weaker configurations may expose credentials and message data in transit.
    /// </remarks>
    Tls,
    /// <remarks>
    /// Selects how the library should negotiate or require transport security when sending
    /// email over the network.
    ///
    /// For production systems, <see cref="SecurityEnum.Tls"/> is strongly recommended
    /// unless you have a specific, validated reason to use a different option.
    /// Unencrypted or weaker configurations may expose credentials and message data in transit.
    /// </remarks>
    Auto
}
