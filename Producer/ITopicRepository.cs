using System.Threading.Tasks;

public interface ITopicRepository 
{
    Task TryCreateTopic(string topicName);
}