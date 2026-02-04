namespace OllyWP.Core.Domain.Enums;


/// <summary>
/// Push notification service types based on endpoint
/// </summary>
public enum PushServiceType
{
    /// <summary>
    /// Firebase Cloud Messaging (Google Chrome, most browsers on Android)
    /// </summary>
    FCM = 0,
    
    /// <summary>
    /// Apple Push Notification Service (Safari on macOS/iOS, all browsers on iOS 16.4+)
    /// </summary>
    ApplePush = 1,
    
    /// <summary>
    /// Mozilla Push Service (Firefox on all platforms)
    /// </summary>
    MozillaPush = 2,
    
    /// <summary>
    /// Windows Push Notification Service (Edge on Windows)
    /// </summary>
    WindowsWNS = 3,
    
    /// <summary>
    /// Huawei Push Kit (HarmonyOS devices)
    /// </summary>
    HuaweiPush = 4,
    
    /// <summary>
    /// Generic/Unknown push service
    /// </summary>
    Generic = 99
}