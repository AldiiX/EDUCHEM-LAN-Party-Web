namespace AdminSphere.Attributes;

/**
 * Attribute který označuje, že metoda má v sobě zabudovaný RequireAuthProcess
 */
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class RequireAuthIsInMethodAttribute : Attribute { }