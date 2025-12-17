using System.Collections.ObjectModel;

namespace TestModel
{
    public interface IIdentifiable
    {
        Guid Id { get; }
    }

    public interface IVersioned
    {
        long Version { get; }
    }

    public interface IAudited
    {
        DateTime CreatedAtUtc { get; }
        DateTime? UpdatedAtUtc { get; }
    }

    public interface ISoftDeleted
    {
        bool IsDeleted { get; }
    }

    public interface INamed
    {
        string Name { get; }
    }

    public interface IWithMetadata
    {
        IReadOnlyDictionary<string, string> Metadata { get; }
    }

    public interface IAggregateRoot : IIdentifiable, IVersioned, IAudited
    {
    }

    public interface IEntity : IIdentifiable, IAudited
    {
    }

    public interface IHasParent<out TParent>
    {
        TParent? Parent { get; }
    }

    public interface IHasChildren<out TChild>
    {
        IReadOnlyList<TChild> Children { get; }
    }

    public interface INode<out TNode> : IHasParent<TNode>, IHasChildren<TNode>
    {
    }

    public interface IResult<out TValue>
    {
        bool IsSuccess { get; }
        TValue Value { get; }
        string? ErrorMessage { get; }
    }

    public readonly struct Result<TValue> : IResult<TValue>
    {
        public bool IsSuccess { get; }
        public TValue Value { get; }
        public string? ErrorMessage { get; }

        private Result(bool isSuccess, TValue value, string? errorMessage)
        {
            IsSuccess = isSuccess;
            Value = value;
            ErrorMessage = errorMessage;
        }

        public static Result<TValue> Success(TValue value)
        {
            return new Result<TValue>(true, value, null);
        }

        public static Result<TValue> Failure(string errorMessage)
        {
            return new Result<TValue>(false, default!, errorMessage);
        }
    }

    public interface IHandler<in TRequest, out TResponse>
    {
        TResponse Handle(TRequest request);
    }

    public interface IAsyncHandler<in TRequest, TResponse>
    {
        Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
    }

    public interface IHasIndex<in TKey, out TValue>
    {
        TValue this[TKey key] { get; }
    }

    public interface IWritableIndex<in TKey, TValue> : IHasIndex<TKey, TValue>
    {
        new TValue this[TKey key] { get; set; }
    }

    public interface IEquatableById<in T>
    {
        bool EqualsById(T other);
    }

    public interface IBuilder<out T>
    {
        T Build();
    }

    public interface IFactory<out T>
    {
        T Create();
    }

    public interface IConfigurable<in TConfiguration>
    {
        void Configure(TConfiguration configuration);
    }

    public interface IResettable
    {
        void Reset();
    }

    public interface ILoggable
    {
        string ToLogString()
        {
            return GetType().FullName ?? GetType().Name;
        }
    }

    public interface IStaticFactory<TSelf>
        where TSelf : IStaticFactory<TSelf>
    {
        static abstract TSelf CreateDefault();
    }

    public abstract class IdentifiableBase : IIdentifiable
    {
        public Guid Id { get; }

        protected IdentifiableBase(Guid id)
        {
            Id = id;
        }
    }

    public abstract class AuditedBase : IdentifiableBase, IAudited
    {
        public DateTime CreatedAtUtc { get; }
        public DateTime? UpdatedAtUtc { get; }

        protected AuditedBase(Guid id, DateTime createdAtUtc, DateTime? updatedAtUtc)
            : base(id)
        {
            CreatedAtUtc = createdAtUtc;
            UpdatedAtUtc = updatedAtUtc;
        }
    }

    public abstract class VersionedAggregateRootBase : AuditedBase, IAggregateRoot
    {
        public long Version { get; }

        protected VersionedAggregateRootBase(Guid id, long version, DateTime createdAtUtc, DateTime? updatedAtUtc)
            : base(id, createdAtUtc, updatedAtUtc)
        {
            Version = version;
        }
    }

    public abstract class NamedEntityBase : AuditedBase, INamed, IEntity
    {
        public string Name { get; }

        protected NamedEntityBase(Guid id, string name, DateTime createdAtUtc, DateTime? updatedAtUtc)
            : base(id, createdAtUtc, updatedAtUtc)
        {
            Name = name;
        }
    }

    public abstract class SoftDeletedEntityBase : NamedEntityBase, ISoftDeleted
    {
        public bool IsDeleted { get; }

        protected SoftDeletedEntityBase(Guid id, string name, DateTime createdAtUtc, DateTime? updatedAtUtc, bool isDeleted)
            : base(id, name, createdAtUtc, updatedAtUtc)
        {
            IsDeleted = isDeleted;
        }
    }

    public record MetadataEnvelope(IReadOnlyDictionary<string, string> Metadata);

    public interface IWithEnvelope
    {
        MetadataEnvelope Envelope { get; }
    }

    public abstract class WithMetadataBase : SoftDeletedEntityBase, IWithMetadata, IWithEnvelope
    {
        public MetadataEnvelope Envelope { get; }

        public IReadOnlyDictionary<string, string> Metadata
        {
            get
            {
                return Envelope.Metadata;
            }
        }

        protected WithMetadataBase(
            Guid id,
            string name,
            DateTime createdAtUtc,
            DateTime? updatedAtUtc,
            bool isDeleted,
            MetadataEnvelope envelope)
            : base(id, name, createdAtUtc, updatedAtUtc, isDeleted)
        {
            Envelope = envelope;
        }
    }

    public interface IUser : IEntity, INamed, IWithMetadata
    {
        string EmailAddress { get; }
    }

    public interface IPrivilegedUser : IUser
    {
        IReadOnlyCollection<string> Permissions { get; }
    }

    public interface ICustomer : IUser
    {
        int LoyaltyPoints { get; }
    }

    public abstract class UserBase : WithMetadataBase, IUser, IEquatableById<IUser>, ILoggable
    {
        public string EmailAddress { get; }

        protected UserBase(
            Guid id,
            string name,
            string emailAddress,
            DateTime createdAtUtc,
            DateTime? updatedAtUtc,
            bool isDeleted,
            MetadataEnvelope envelope)
            : base(id, name, createdAtUtc, updatedAtUtc, isDeleted, envelope)
        {
            EmailAddress = emailAddress;
        }

        bool IEquatableById<IUser>.EqualsById(IUser other)
        {
            if (other == null)
            {
                return false;
            }

            return other.Id == Id;
        }
    }

    public class CustomerUser : UserBase, ICustomer
    {
        public int LoyaltyPoints { get; }

        public CustomerUser(
            Guid id,
            string name,
            string emailAddress,
            DateTime createdAtUtc,
            DateTime? updatedAtUtc,
            bool isDeleted,
            MetadataEnvelope envelope,
            int loyaltyPoints)
            : base(id, name, emailAddress, createdAtUtc, updatedAtUtc, isDeleted, envelope)
        {
            LoyaltyPoints = loyaltyPoints;
        }
    }

    public sealed class AdminUser : UserBase, IPrivilegedUser
    {
        public IReadOnlyCollection<string> Permissions { get; }

        public AdminUser(
            Guid id,
            string name,
            string emailAddress,
            DateTime createdAtUtc,
            DateTime? updatedAtUtc,
            bool isDeleted,
            MetadataEnvelope envelope,
            IReadOnlyCollection<string> permissions)
            : base(id, name, emailAddress, createdAtUtc, updatedAtUtc, isDeleted, envelope)
        {
            Permissions = permissions;
        }
    }

    public enum OrderState
    {
        Unknown = 0,
        Created = 1,
        Paid = 2,
        Shipped = 3,
        Cancelled = 4
    }

    public interface IOrder : IAggregateRoot, ISoftDeleted, IWithMetadata
    {
        Guid CustomerUserId { get; }
        decimal TotalAmount { get; }
        OrderState State { get; }
    }

    public abstract class OrderBase : VersionedAggregateRootBase, IOrder, IWithEnvelope
    {
        public bool IsDeleted { get; }
        public Guid CustomerUserId { get; }
        public decimal TotalAmount { get; }
        public OrderState State { get; }
        public MetadataEnvelope Envelope { get; }

        public IReadOnlyDictionary<string, string> Metadata
        {
            get
            {
                return Envelope.Metadata;
            }
        }

        protected OrderBase(
            Guid id,
            long version,
            DateTime createdAtUtc,
            DateTime? updatedAtUtc,
            bool isDeleted,
            Guid customerUserId,
            decimal totalAmount,
            OrderState state,
            MetadataEnvelope envelope)
            : base(id, version, createdAtUtc, updatedAtUtc)
        {
            IsDeleted = isDeleted;
            CustomerUserId = customerUserId;
            TotalAmount = totalAmount;
            State = state;
            Envelope = envelope;
        }
    }

    public sealed class OnlineOrder : OrderBase
    {
        public Uri CallbackUri { get; }

        public OnlineOrder(
            Guid id,
            long version,
            DateTime createdAtUtc,
            DateTime? updatedAtUtc,
            bool isDeleted,
            Guid customerUserId,
            decimal totalAmount,
            OrderState state,
            MetadataEnvelope envelope,
            Uri callbackUri)
            : base(
                id,
                version,
                createdAtUtc,
                updatedAtUtc,
                isDeleted,
                customerUserId,
                totalAmount,
                state,
                envelope)
        {
            CallbackUri = callbackUri;
        }
    }

    public sealed class InStoreOrder : OrderBase
    {
        public string StoreLocationCode { get; }

        public InStoreOrder(
            Guid id,
            long version,
            DateTime createdAtUtc,
            DateTime? updatedAtUtc,
            bool isDeleted,
            Guid customerUserId,
            decimal totalAmount,
            OrderState state,
            MetadataEnvelope envelope,
            string storeLocationCode)
            : base(
                id,
                version,
                createdAtUtc,
                updatedAtUtc,
                isDeleted,
                customerUserId,
                totalAmount,
                state,
                envelope)
        {
            StoreLocationCode = storeLocationCode;
        }
    }

    public interface IRepository<TEntity, in TId>
        where TEntity : IIdentifiable
    {
        Result<TEntity> GetById(TId id);
    }

    public interface IMutableRepository<TEntity, in TId> : IRepository<TEntity, TId>
        where TEntity : IIdentifiable
    {
        Result<TEntity> Add(TEntity entity);
        Result<TEntity> Update(TEntity entity);
    }

    public abstract class RepositoryBase<TEntity, TId> : IMutableRepository<TEntity, TId>
        where TEntity : IIdentifiable
    {
        public abstract Result<TEntity> GetById(TId id);
        public abstract Result<TEntity> Add(TEntity entity);
        public abstract Result<TEntity> Update(TEntity entity);
    }

    public sealed class InMemoryRepository<TEntity> : RepositoryBase<TEntity, Guid>, IWritableIndex<Guid, TEntity>
        where TEntity : IIdentifiable
    {
        private readonly Dictionary<Guid, TEntity> storage;

        public InMemoryRepository()
        {
            storage = new Dictionary<Guid, TEntity>();
        }

        public override Result<TEntity> GetById(Guid id)
        {
            if (storage.TryGetValue(id, out var entity))
            {
                return Result<TEntity>.Success(entity);
            }

            return Result<TEntity>.Failure("Not found.");
        }

        public override Result<TEntity> Add(TEntity entity)
        {
            if (entity == null)
            {
                return Result<TEntity>.Failure("Entity is null.");
            }

            if (storage.ContainsKey(entity.Id))
            {
                return Result<TEntity>.Failure("Already exists.");
            }

            storage.Add(entity.Id, entity);
            return Result<TEntity>.Success(entity);
        }

        public override Result<TEntity> Update(TEntity entity)
        {
            if (entity == null)
            {
                return Result<TEntity>.Failure("Entity is null.");
            }

            if (!storage.ContainsKey(entity.Id))
            {
                return Result<TEntity>.Failure("Not found.");
            }

            storage[entity.Id] = entity;
            return Result<TEntity>.Success(entity);
        }

        public TEntity this[Guid key]
        {
            get
            {
                return storage[key];
            }
            set
            {
                storage[key] = value;
            }
        }

        TEntity IHasIndex<Guid, TEntity>.this[Guid key]
        {
            get
            {
                return storage[key];
            }
        }
    }

    public interface ITreeNode : INode<ITreeNode>, IIdentifiable, INamed
    {
    }

    public abstract class TreeNodeBase : IdentifiableBase, ITreeNode
    {
        private readonly List<ITreeNode> children;

        public string Name { get; }

        public ITreeNode? Parent { get; private set; }

        public IReadOnlyList<ITreeNode> Children
        {
            get
            {
                return children;
            }
        }

        protected TreeNodeBase(Guid id, string name)
            : base(id)
        {
            Name = name;
            children = new List<ITreeNode>();
        }

        protected void AttachChild(ITreeNode child)
        {
            if (child == null)
            {
                throw new ArgumentNullException(nameof(child));
            }

            children.Add(child);

            if (child is TreeNodeBase treeNodeBase)
            {
                treeNodeBase.Parent = this;
            }
        }
    }

    public sealed class FolderNode : TreeNodeBase
    {
        public FolderNode(Guid id, string name)
            : base(id, name)
        {
        }

        public void Add(ITreeNode child)
        {
            AttachChild(child);
        }
    }

    public sealed class FileNode : TreeNodeBase
    {
        public long SizeInBytes { get; }

        public FileNode(Guid id, string name, long sizeInBytes)
            : base(id, name)
        {
            SizeInBytes = sizeInBytes;
        }
    }

    public abstract class ComponentBase : IResettable, IConfigurable<ComponentBase.Configuration>
    {
        private Configuration configuration;

        protected ComponentBase()
        {
            configuration = Configuration.CreateDefault();
        }

        void IResettable.Reset()
        {
            configuration = Configuration.CreateDefault();
            ResetCore();
        }

        void IConfigurable<Configuration>.Configure(Configuration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            this.configuration = configuration;
            ConfigureCore(configuration);
        }

        protected abstract void ResetCore();
        protected abstract void ConfigureCore(Configuration configuration);

        public sealed class Configuration : IStaticFactory<Configuration>
        {
            public string Mode { get; }
            public int Level { get; }

            public Configuration(string mode, int level)
            {
                Mode = mode;
                Level = level;
            }

            public static Configuration CreateDefault()
            {
                return new Configuration("Default", 0);
            }
        }
    }

    public interface IProcessor<in TInput, out TOutput> : IHandler<TInput, TOutput>
    {
    }

    public abstract class ProcessorBase<TInput, TOutput> : IProcessor<TInput, TOutput>
    {
        public abstract TOutput Handle(TInput request);
    }

    public sealed class PipelineProcessor<TInput, TMiddle, TOutput> : ProcessorBase<TInput, TOutput>
    {
        private readonly IProcessor<TInput, TMiddle> first;
        private readonly IProcessor<TMiddle, TOutput> second;

        public PipelineProcessor(IProcessor<TInput, TMiddle> first, IProcessor<TMiddle, TOutput> second)
        {
            this.first = first;
            this.second = second;
        }

        public override TOutput Handle(TInput request)
        {
            var middle = first.Handle(request);
            return second.Handle(middle);
        }
    }

    public sealed class IdentityProcessor<T> : ProcessorBase<T, T>
    {
        public override T Handle(T request)
        {
            return request;
        }
    }

    public interface IComparerProvider<in T>
    {
        IComparer<T> CreateComparer();
    }

    public sealed class ComparerProvider<T> : IComparerProvider<T>
    {
        private readonly Comparison<T> comparison;

        public ComparerProvider(Comparison<T> comparison)
        {
            this.comparison = comparison;
        }

        public IComparer<T> CreateComparer()
        {
            return Comparer<T>.Create(comparison);
        }
    }

    public partial class SplitAcrossPartials : IIdentifiable
    {
        public Guid Id { get; }

        public SplitAcrossPartials(Guid id)
        {
            Id = id;
        }
    }

    public partial class SplitAcrossPartials : INamed
    {
        public string Name
        {
            get
            {
                return "PartialName";
            }
        }
    }

    public abstract class ShadowingBase
    {
        public virtual string Value
        {
            get
            {
                return "Base";
            }
        }
    }

    public class ShadowingDerived : ShadowingBase
    {
        public new string Value
        {
            get
            {
                return "DerivedHidden";
            }
        }
    }

    public sealed class ShadowingDerivedOverride : ShadowingBase
    {
        public override string Value
        {
            get
            {
                return "DerivedOverride";
            }
        }
    }

    public readonly struct StrongId : IEquatable<StrongId>
    {
        public Guid Value { get; }

        public StrongId(Guid value)
        {
            Value = value;
        }

        public bool Equals(StrongId other)
        {
            return other.Value == Value;
        }

        public override bool Equals(object? obj)
        {
            if (obj is StrongId other)
            {
                return Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(StrongId left, StrongId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StrongId left, StrongId right)
        {
            return !left.Equals(right);
        }
    }

    public interface IStronglyIdentified
    {
        StrongId StrongId { get; }
    }

    public abstract class StronglyIdentifiedBase : IStronglyIdentified
    {
        public StrongId StrongId { get; }

        protected StronglyIdentifiedBase(StrongId strongId)
        {
            StrongId = strongId;
        }
    }

    public sealed class StrongCustomer : StronglyIdentifiedBase, ICustomer, IAudited, INamed, ISoftDeleted
    {
        public Guid Id
        {
            get
            {
                return StrongId.Value;
            }
        }

        public string Name { get; }
        public string EmailAddress { get; }
        public int LoyaltyPoints { get; }
        public DateTime CreatedAtUtc { get; }
        public DateTime? UpdatedAtUtc { get; }

        public StrongCustomer(
            StrongId strongId,
            string name,
            string emailAddress,
            int loyaltyPoints,
            DateTime createdAtUtc,
            DateTime? updatedAtUtc)
            : base(strongId)
        {
            Name = name;
            EmailAddress = emailAddress;
            LoyaltyPoints = loyaltyPoints;
            CreatedAtUtc = createdAtUtc;
            UpdatedAtUtc = updatedAtUtc;
        }

        IReadOnlyDictionary<string, string> IWithMetadata.Metadata
        {
            get
            {
                return new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
            }
        }

        bool ISoftDeleted.IsDeleted
        {
            get
            {
                return false;
            }
        }
    }

    public static class Extensions
    {
        public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source is IReadOnlyList<T> list)
            {
                return list;
            }

            return new List<T>(source).AsReadOnly();
        }
    }
}
