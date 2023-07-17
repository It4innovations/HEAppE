using System;
using System.Collections;
using System.Linq;

namespace HEAppE.Utils
{
    /// <summary>
    /// Object Extensions methods
    /// </summary>
    public static class ObjectExtension
    {
        /// <summary>
        /// Generic get object hash code
        /// Not suitable for circular relations objects
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns></returns>
        public static int GetObjectHashCode(this object obj)
        {
            int hash = 0;

            //Collection
            if (obj.GetType().GetInterfaces().FirstOrDefault(s => s.Name == "IEnumerable") != null)
            {
                var objCollectionType = obj.GetType().IsGenericType
                        ? obj.GetType().GetGenericArguments().FirstOrDefault()
                        : obj.GetType().GetElementType();

                if (objCollectionType == null)
                {
                    return hash;
                }

                var objCollection = obj as IEnumerable;
                foreach (var collectionItem in objCollection)
                {
                    if (!objCollectionType.IsClass || objCollectionType == typeof(string))
                    {
                        hash = HashCode.Combine(collectionItem, hash);
                    }
                    else
                    {
                        if (collectionItem == null)
                        {
                            hash = HashCode.Combine(collectionItem, hash);
                        }
                        else
                        {
                            hash = HashCode.Combine(collectionItem.GetHashCode(), hash);
                        }
                    }
                }
                return hash;
            }

            //Properties
            foreach (var property in obj.GetType().GetProperties())
            {
                var objValue = property.GetValue(obj);
                if (!property.PropertyType.IsClass || property.PropertyType == typeof(string))
                {
                    hash = HashCode.Combine(objValue, hash);
                }
                else
                {
                    if (objValue == null)
                    {
                        hash = HashCode.Combine(objValue, hash);
                    }
                    else
                    {
                        hash = HashCode.Combine(objValue.GetHashCode(), hash);
                    }
                }
            }
            return hash;
        }

        /// <summary>
        /// Generic Get object hash code recursive
        /// Not suitable for circular relations objects
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns></returns>
        public static int GetObjectHashCodeRecursive(this object obj)
        {
            int hash = 0;
            //Collection
            if (obj.GetType().GetInterfaces().FirstOrDefault(s => s.Name == "IEnumerable") != null)
            {
                var objCollectionType = obj.GetType().IsGenericType
                        ? obj.GetType().GetGenericArguments().FirstOrDefault()
                        : obj.GetType().GetElementType();

                if (objCollectionType == null)
                {
                    return hash;
                }

                var objCollection = obj as IEnumerable;

                foreach (var collectionItem in objCollection)
                {
                    if (!objCollectionType.IsClass || objCollectionType == typeof(string))
                    {
                        hash = HashCode.Combine(collectionItem, hash);
                    }
                    else
                    {
                        if (collectionItem == null)
                        {
                            hash = HashCode.Combine(collectionItem, hash);
                        }
                        else
                        {
                            hash = HashCode.Combine(GetObjectHashCodeRecursive(collectionItem), hash);
                        }
                    }
                }
                return hash;
            }

            //Properties
            foreach (var property in obj.GetType().GetProperties())
            {
                var objValue = property.GetValue(obj);
                if (!property.PropertyType.IsClass || property.PropertyType == typeof(string))
                {
                    hash = HashCode.Combine(objValue, hash);
                }
                else
                {
                    if (objValue == null)
                    {
                        hash = HashCode.Combine(objValue, hash);
                    }
                    else
                    {
                        hash = HashCode.Combine(GetObjectHashCodeRecursive(objValue), hash);
                    }
                }
            }
            return hash;
        }

        /// <summary>
        /// Generic Compare recursive objects
        /// Not suitable for circular relations objects
        /// </summary>
        /// <param name="obj">Object</param>
        /// <param name="another">Another object</param>
        /// <returns></returns>
        public static bool CompareRecursive(this object obj, object another)
        {
            if (ReferenceEquals(obj, another))
            {
                return true;
            }

            if (obj == null && another == null)
            {
                return true;
            }

            if (obj == null || another == null)
            {
                return false;
            }

            if (obj.GetType() != another.GetType())
            {
                return false;
            }

            if (obj.GetType().GetInterfaces().FirstOrDefault(s => s.Name == "IEnumerable") != null)
            {
                return CompareCollections(obj as ICollection, another as ICollection);
            }

            foreach (var property in obj.GetType().GetProperties())
            {
                var objValue = property.GetValue(obj);
                var anotherValue = another.GetType().GetProperty(property.Name).GetValue(another);
                if (!property.PropertyType.IsClass || property.PropertyType == typeof(string))
                {
                    if (objValue is null || anotherValue is null)
                    {
                        if (objValue != anotherValue)
                        {
                            return false;
                        }
                    }
                    else if (!objValue.Equals(anotherValue))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!CompareRecursive(objValue, anotherValue))
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Generic compare collections
        /// Not suitable for circular relations objects
        /// </summary>
        /// <param name="objCollection">Collection</param>
        /// <param name="anotherCollection">Another collection</param>
        /// <returns></returns>
        private static bool CompareCollections(ICollection objCollection, ICollection anotherCollection)
        {

            var objCollectionType = objCollection.GetType().IsGenericType
                         ? objCollection.GetType().GetGenericArguments().FirstOrDefault()
                         : objCollection.GetType().GetElementType();


            var anotherCollectionType = anotherCollection.GetType().IsGenericType
                         ? anotherCollection.GetType().GetGenericArguments().FirstOrDefault()
                         : anotherCollection.GetType().GetElementType();

            if (objCollectionType == null || anotherCollectionType == null)
            {
                return false;
            }

            if (objCollectionType.GetType() != anotherCollectionType.GetType())
            {
                return false;
            }

            if (objCollection.Count != anotherCollection.Count)
            {
                return false;
            }

            IEnumerator objCollectionEnumerator = objCollection.GetEnumerator();
            IEnumerator anotherCollectionEnumerator = anotherCollection.GetEnumerator();

            while (objCollectionEnumerator.MoveNext())
            {
                if (anotherCollectionEnumerator.MoveNext())
                {

                    if (!objCollectionType.IsClass || objCollectionType == typeof(string))
                    {
                        if (objCollectionEnumerator.Current is null || anotherCollectionEnumerator.Current is null)
                        {
                            if (objCollectionEnumerator.Current != anotherCollectionEnumerator.Current)
                            {
                                return false;
                            }
                        }
                        else if (!objCollectionEnumerator.Current.Equals(anotherCollectionEnumerator.Current))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return CompareRecursive(objCollectionEnumerator.Current, anotherCollectionEnumerator.Current);
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
    }
}
