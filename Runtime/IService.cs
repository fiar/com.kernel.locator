
namespace Kernel.ServiceLocator
{
	public interface IService
	{
		// On Create Instance
		void Awake();
		// On Destroy Instance (Application quit)
		void Destroy();
		// On Load Scene
		void Reset();
	}
}
