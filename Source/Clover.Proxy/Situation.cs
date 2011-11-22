namespace Clover.Proxy
{
    public enum Situation
    {
        UnSerializable = 2,
        Array = 4,
        IEnumableT = 8,
        //NullableT = 17,

        Dictionary = 32,


        Serializable = 1,
        SerializableArray = 5,
        SerializableIEnumableT = 9,
        SerializableNullableT = 17,
        SerializableDirtionary = 33,

        SerializableEnum = 65,
    }
}