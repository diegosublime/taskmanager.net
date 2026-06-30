using System.Collections.ObjectModel;

namespace taskmanager.api
{
    //TODO: Pending to create abstraction for the entity or aggregate, to re-use
    //the domain events logic in other entities
    public class ListTask
    {
        //TODO: public just for json serializer 
        public ListTask(string id, string userId, string name, string description, string purpose)
        {
            Id = id;
            UserId = userId;
            Name = name;
            Description = description;
            Purpose = purpose;
        }
        private List<DomainEvent> _DomainEvents { get; set; } = [];

        public string Id { get; }
        public string UserId { get; }
        public string Name { get; }
        public string Description { get; }
        public string Purpose { get; }

        public ReadOnlyCollection<DomainEvent> GetDomainEvents() 
        { 
            return _DomainEvents.AsReadOnly(); 
        }

        public void RiseDomainEvent(DomainEvent domainEvent) 
        {
            _DomainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents() 
        {
            _DomainEvents.Clear();
        }
        
        public static ListTask Create(string id, string userId, string name, string description, string purpose)
        {
            var newListTask = new ListTask(id, userId, name, description, purpose);

            //Rise domain event
            newListTask.RiseDomainEvent(new ListTaskCreatedEvent(id));

            return newListTask;
        }
    }
}
