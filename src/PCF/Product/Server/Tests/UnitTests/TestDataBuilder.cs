namespace PCF.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Xunit;

    /// <summary>
    /// Defines base type for test data builders.
    /// </summary>
    public abstract class TestDataBuilder<T>
    {
        private readonly List<Action<T>> withList = new List<Action<T>>();

        /// <summary>
        /// Builds data object according to spec.
        /// </summary>
        /// <returns>Data object.</returns>
        public T Build()
        {
            T item = this.CreateNewObject();

            foreach (var action in this.withList)
            {
                action(item);
            }

            return item;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="TestDataBuilder{T}" />.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator T(TestDataBuilder<T> builder)
        {
            return builder.Build();
        }

        /// <summary>
        /// Sets the given property on the given object. The given expression must be a lambda that assigns a property on the object.
        /// </summary>
        public TestDataBuilder<T> With<TV>(Expression<Func<T, TV>> lambda, TV value) where TV : class
        {
            return this.UnsafeWith(lambda, value);
        }

        /// <summary>
        /// Sets the given property on the given object. The given expression must be a lambda that assigns a property on the object.
        /// </summary>
        public TestDataBuilder<T> WithArray<TV>(Expression<Func<T, TV[]>> lambda, params TV[] value)
        {
            return this.UnsafeWith(lambda, value);
        }

        /// <summary>
        /// Sets the given property on the given object. The given expression must be a lambda that assigns a property on the object.
        /// </summary>
        public TestDataBuilder<T> WithValue<TV>(Expression<Func<T, TV>> lambda, TV value) where TV : struct
        {
            return this.UnsafeWith(lambda, value);
        }

        /// <summary>
        /// Sets the given property on the given object. The given expression must be a lambda that assigns a property on the object.
        /// </summary>
        public TestDataBuilder<T> WithValue<TV>(Expression<Func<T, TV?>> lambda, TV? value) where TV : struct
        {
            return this.UnsafeWith(lambda, value);
        }

        /// <summary>
        /// Sets the given property on the given object. The given expression must be a lambda that assigns a property on the object.
        /// </summary>
        protected TestDataBuilder<T> UnsafeWith<TP>(Expression<Func<T, TP>> lambda, TP value)
        {
            // Do some basic validation here to make sure we're doing the right things.
            // IF YOU ARE GETTING A CAST ERROR IN THIS METHOD, YOU ARE PROBABLY DOING SOMETHING WRONG.
            Assert.Equal(ExpressionType.Lambda, lambda.NodeType);
            ParameterExpression parameter = lambda.Parameters.Single();

            var memberExpression = (MemberExpression)lambda.Body;
            PropertyInfo propertyInfo = (PropertyInfo)memberExpression.Member;
            Assert.Equal(memberExpression.Expression, parameter);

            this.withList.Add(t => propertyInfo.SetValue(t, value));

            return this;
        }

        /// <summary>
        /// Creates an instance of T with the default values.
        /// </summary>
        protected abstract T CreateNewObject();
    }
}