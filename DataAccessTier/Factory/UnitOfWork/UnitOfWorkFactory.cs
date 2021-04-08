using System;
using System.Collections.Generic;
using HEAppE.DataAccessTier.UnitOfWork;

namespace HEAppE.DataAccessTier.Factory.UnitOfWork
{
    public abstract class UnitOfWorkFactory
    {
        #region Instances
        private static readonly Dictionary<UnitOfWorkType, UnitOfWorkFactory> _factoryInstances =
            new Dictionary<UnitOfWorkType, UnitOfWorkFactory>(Enum.GetValues(typeof(UnitOfWorkType)).Length);
        #endregion
        #region Methods
        public static UnitOfWorkFactory GetUnitOfWorkFactory()
        {
            return GetUnitOfWorkFactory(UnitOfWorkType.Database);
        }

        public static UnitOfWorkFactory GetUnitOfWorkFactory(UnitOfWorkType type)
        {
            lock (_factoryInstances)
            {
                if (!_factoryInstances.ContainsKey(type))
                {
                    _factoryInstances.Add(type, CreateUnitOfWorkFactory(type));
                }
            }
            return _factoryInstances[type];
        }

        private static UnitOfWorkFactory CreateUnitOfWorkFactory(UnitOfWorkType type)
        {
            switch (type)
            {
                case UnitOfWorkType.Database:
                    return new DatabaseUnitOfWorkFactory();
            }
            throw new ArgumentException("Unit of work factory for type " + type +
                                        " is not implemented. Check the switch in HaaSMiddleware.DataAccessLayer.Factory.UnitOfWork.AbstractUnitOfWorkFactory.CreateUnitOfWorkFactory(UnitOfWorkType type) method.");
        }
        #endregion
        #region Abstract Methods
        public abstract IUnitOfWork CreateUnitOfWork();
        #endregion
    }
}