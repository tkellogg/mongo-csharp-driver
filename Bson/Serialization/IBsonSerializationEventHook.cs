using System;
using System.Collections.Generic;
using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Exposes events in the deserialization process to enable runtime migration schemes
    /// to work.
    /// </summary>
    public interface IBsonSerializationEventHook
    {
        /// <summary>
        /// Initialize state data for the current document. Each sub-document gets it's own state.
        /// </summary>
        /// <param name="actualType"></param>
        /// <returns></returns>
        IBsonSerializerEventHookContext CreateContext(Type actualType);

        /// <summary>
        /// This is called as the BsonClassMapSerializer arrives at an element, before it processes it. 
        /// If there was a <c>_version</c> element earlier in the document, pass that value as 
        /// documentVersion, otherwise pass 0. This can be used to change a field type (i.e. changing 
        /// from enum to a complex object). 
        /// </summary>
        /// <param name="memberMap"></param>
        /// <param name="actualType"></param>
        /// <param name="documentVersion">
        /// The version of the top-level document; 0 if not specified, null if not participating in versioning
        /// </param>
        /// <returns>
        /// The serializer that should process the element, otherwise <c>SerializerSelection.Empty</c> 
        /// if it has no opinion.
        /// </returns>
        SerializerSelection SelectElementDeserializer(BsonMemberMap memberMap, Type actualType, int? documentVersion);

        /// <summary>
        /// This is called after an object has been constructed and there were extra BSON elements that
        /// weren't accounted for by members in the type (<c>extraElements</c>).
        /// </summary>
        /// <param name="constructedObject">The object that has most recently been constructed.</param>
        /// <param name="context">Implementation-specific context - for holding state data like extra elements</param>
        /// <param name="version">The version of the top-level document; 0 if not specified.</param>
        void ProcessExtraElements(object constructedObject, IBsonSerializerEventHookContext context, int version);

        /// <summary>
        /// Indicates that this event hook knows what to do with extra elements for the given type.
        /// </summary>
        /// <param name="nominalType"></param>
        /// <param name="documentVersion"></param>
        /// <returns></returns>
        bool CanRecoverData(Type nominalType, int? documentVersion);
    }

    /// <summary>
    /// Container for representing a BSON serializer that should be used for deserialization, as well 
    /// as the callback for assigning the result into the object graph. Values of this type are immutable.
    /// </summary>
    public struct SerializerSelection
    {
        private readonly IBsonSerializer _serializer;
        private readonly Action<object, object> _assignmentOperation;
        private readonly Type _actualType;

        /// <summary>
        /// Initializes an instance of SerializerSelection
        /// </summary>
        /// <param name="serializer">The selected serializer. Should be null if no serializer could be selected</param>
        /// <param name="assignmentOperation">
        /// The function to assign the newly deserialized value (result of IBsonSerializer.Deserialize) 
        /// to the parent object. Parameters are (<c>parentObject</c>, <c>propertyValue</c>).
        /// </param>
        public SerializerSelection(IBsonSerializer serializer, Action<object, object> assignmentOperation, Type actualType)
        {
            _serializer = serializer;
            _assignmentOperation = assignmentOperation;
            _actualType = actualType;
        }

        /// <summary>
        /// Gets the selected BSON Serializer that should be used for deserialization.
        /// </summary>
        public IBsonSerializer Serializer { get { return _serializer; } }

        /// <summary>
        /// Gets the actual type that the element should be deserialized as.
        /// </summary>
        public Type ActualType { get { return _actualType; } }

        /// <summary>
        /// Assign the newly deserialized value (result of IBsonSerializer.Deserialize) to the parent 
        /// object. 
        /// </summary>
        /// <param name="parent">
        /// The fully constructed parent object that presumably contains the property that the value must
        /// be assigned to.
        /// </param>
        /// <param name="value">The fully constructed result from Serializer.Deserialize</param>
        public void AssignValueToParentObject(object parent, object value)
        {
            if (_assignmentOperation != null)
                _assignmentOperation(parent, value);
        }

        /// <summary>
        /// Represents the empty SerializerSelection. This field is readonly.
        /// </summary>
        public static readonly SerializerSelection Empty;

        private static IBsonSerializationEventHook __current;

        /// <summary>
        /// Gets or sets the serializer event hook. Can never be null.
        /// </summary>
        public static IBsonSerializationEventHook Current
        {
            get { return __current; }
            set
            {
                if (value == null) 
                    throw new ArgumentNullException("value");

                __current = value;
            }
        }

        static SerializerSelection()
        {
            Current = new DefaultSerializationEventHook();
        }
    }

    internal class DefaultSerializationEventHook : IBsonSerializationEventHook
    {
        public IBsonSerializerEventHookContext CreateContext(Type actualType)
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(EventContext)))
            {
                BsonClassMap.RegisterClassMap<EventContext>();
            }
            var classMap = BsonClassMap.LookupClassMap(typeof (EventContext));
            var memberInfo = typeof (EventContext).GetProperty("ExtraMembers");
            return new EventContext(new BsonMemberMap(classMap, memberInfo));
        }

        public SerializerSelection SelectElementDeserializer(BsonMemberMap memberMap, Type actualType, int? documentVersion)
        {
            return new SerializerSelection(memberMap.GetSerializer(actualType), memberMap.Setter, null);
        }

        public void ProcessExtraElements(object constructedObject, IBsonSerializerEventHookContext context, int version)
        {
        }

        public bool CanRecoverData(Type nominalType, int? documentVersion)
        {
            return false;
        }

        class EventContext : IBsonSerializerEventHookContext
        {
            private readonly BsonMemberMap _memberMap;

            public EventContext(BsonMemberMap memberMap)
            {
                _memberMap = memberMap;
                ExtraElements = new Dictionary<string, object>();
            }

            public BsonMemberMap GetExtraElementsMap(string elementName)
            {
                return _memberMap;
            }
            public object ExtraElementsHost { get { return this; } }
            public Dictionary<string, object> ExtraElements { get; private set; }
        }
    }

    /// <summary>
    /// State data for accruing extra elements in the format that the implementor requires.
    /// </summary>
    public interface IBsonSerializerEventHookContext
    {
        /// <summary></summary>
        BsonMemberMap GetExtraElementsMap(string elementName);

        /// <summary></summary>
        object ExtraElementsHost { get; }
    }
}
