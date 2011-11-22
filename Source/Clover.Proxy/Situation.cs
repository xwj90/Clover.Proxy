namespace Clover.Proxy
{
    public enum Situation
    {
        Unknown = 0,
        UNSerializable = 2,
        Array = 4,
        IEnumerableOfT = 8,
        //NullableT = 17,
        Dictionary = 32,
        Serializable = 1,
        SerializableArray = 5,
        SerializableIEnumerableOfT = 9,
        SerializableNullableT = 17,
        SerializableDictionary = 33,
        SerializableEnum = 65,
    }
}