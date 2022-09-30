namespace WebServer.Extensions
{
    //http://www.scip.be/index.php?Page=ArticlesNET18
    public class HierarchyNode<T>
    {
        public T? Entity { get; set; }
        public IEnumerable<HierarchyNode<T>> ChildNodes { get; set; } = Enumerable.Empty<HierarchyNode<T>>();
        public int Depth { get; set; }
    }
    public static class Hierarchy
    {
        private static IEnumerable<HierarchyNode<TEntity>> CreateHierarchy<TEntity, TProperty>(IEnumerable<TEntity> allItems, TEntity parentItem, Func<TEntity, TProperty> idProperty, Func<TEntity, TProperty> parentIdProperty, int depth) where TEntity : class
        {
            IEnumerable<TEntity> childs;

            if (parentItem == null)
                childs = allItems.Where(i => parentIdProperty(i).Equals(default(TProperty)));
            else
                childs = allItems.Where(i => parentIdProperty(i).Equals(idProperty(parentItem)));

            if (childs.Any())
            {
                depth++;

                foreach (var item in childs)
                    yield return new HierarchyNode<TEntity>()
                    {
                        Entity = item,
                        ChildNodes = CreateHierarchy<TEntity, TProperty>(allItems, item, idProperty, parentIdProperty, depth),
                        Depth = depth
                    };
            }
        }

        /// <summary>
        /// LINQ IEnumerable AsHierachy() extension method
        /// </summary>
        /// <typeparam name="TEntity">Entity class</typeparam>
        /// <typeparam name="TProperty">Property of entity class</typeparam>
        /// <param name="allItems">Flat collection of entities</param>
        /// <param name="idProperty">Reference to Id/Key of entity</param>
        /// <param name="parentIdProperty">Reference to parent Id/Key</param>
        /// <returns>Hierarchical structure of entities</returns>
        public static IEnumerable<HierarchyNode<TEntity>> AsHierarchy<TEntity, TProperty>(this IEnumerable<TEntity> allItems, Func<TEntity, TProperty> idProperty, Func<TEntity, TProperty> parentIdProperty) where TEntity : class
        {
            return CreateHierarchy(allItems, default(TEntity), idProperty, parentIdProperty, 0);
        }
    }
}