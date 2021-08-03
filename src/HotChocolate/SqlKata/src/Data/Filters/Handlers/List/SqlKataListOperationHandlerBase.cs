using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using SqlKata;

namespace HotChocolate.Data.SqlKata.Filters
{
    /// <summary>
    /// The base of a SqlKata operation handler specific for
    /// <see cref="IListFilterInputType"/>
    /// If the <see cref="FilterTypeInterceptor"/> encounters a operation field that implements
    /// <see cref="IListFilterInputType"/> and matches the operation identifier
    /// defined in <see cref="SqlKataComparableOperationHandler.Operation"/> the handler is bound to
    /// the field
    /// </summary>
    public abstract class SqlKataListOperationHandlerBase
        : FilterFieldHandler<SqlKataFilterVisitorContext, Query>
    {
        /// <summary>
        /// Specifies the identifier of the operations that should be handled by this handler
        /// </summary>
        protected abstract int Operation { get; }

        /// <inheritdoc />
        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            return context.Type is IListFilterInputType &&
                fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Id == Operation;
        }

        /// <inheritdoc />
        public override bool TryHandleEnter(
            SqlKataFilterVisitorContext context,
            IFilterField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (node.Value.IsNull())
            {
                context.ReportError(
                    ErrorHelper.CreateNonNullError(field, node.Value, context));

                action = SyntaxVisitor.Skip;
                return true;
            }

            if (context.RuntimeTypes.Count > 0 &&
                context.RuntimeTypes.Peek().TypeArguments is { Count: > 0 } args)
            {
                IExtendedType element = args[0];
                context.RuntimeTypes.Push(element);
                context.AddScope();

                action = SyntaxVisitor.Continue;
                return true;
            }

            action = null;
            return false;
        }

        /// <inheritdoc />
        public override bool TryHandleLeave(
            SqlKataFilterVisitorContext context,
            IFilterField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            context.RuntimeTypes.Pop();

            if (context.TryCreateQuery(out Query? query) &&
                context.Scopes.Pop() is SqlKataFilterScope scope)
            {
                var path = context.GetMongoFilterScope().GetPath();
                Query combinedOperations = HandleListOperation(
                    context,
                    field,
                    scope,
                    path);

                context.GetLevel().Enqueue(combinedOperations);
            }

            action = SyntaxVisitor.Continue;
            return true;
        }

        /// <summary>
        /// Maps a operation field to a SqlKata list filter definition.
        /// This method is called when the <see cref="FilterVisitor{TContext,T}"/> enters a
        /// field
        /// </summary>
        /// <param name="context">The context of the visitor</param>
        /// <param name="field">The currently visited filter field</param>
        /// <param name="scope">The current scope of the visitor</param>
        /// <param name="path">The path that leads to this visitor</param>
        /// <returns></returns>
        protected abstract Query HandleListOperation(
            SqlKataFilterVisitorContext context,
            IFilterField field,
            SqlKataFilterScope scope,
            string path);

        /// <summary>
        /// Combines all definitions of the <paramref name="scope"/> with and
        /// </summary>
        /// <param name="scope">The scope where the definitions should be combined</param>
        /// <returns>A with and combined filter definition of all definitions of the scope</returns>
        protected static Query CombineOperationsOfScope(
            SqlKataFilterScope scope)
        {
            Queue<Query> level = scope.Level.Peek();
            if (level.Count == 1)
            {
                return level.Peek();
            }

            //return new AndFilterDefinition(level.ToArray());
            throw new NotImplementedException();
        }
    }
}