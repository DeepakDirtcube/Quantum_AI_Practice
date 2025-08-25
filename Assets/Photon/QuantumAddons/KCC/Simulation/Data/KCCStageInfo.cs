namespace Quantum
{
	using System.Collections.Generic;
	using Quantum.Collections;

	/// <summary>
	/// Internal helper container for execution of processor callbacks. Do not use directly.
	/// </summary>
	public sealed unsafe class KCCStageInfo
	{
		public List<KCCProcessor>     Processors     = new List<KCCProcessor>();
		public List<KCCProcessorInfo> ProcessorInfos = new List<KCCProcessorInfo>();

		public void Reset()
		{
			Processors.Clear();
			ProcessorInfos.Clear();
		}

		public void CacheProcessors(KCCContext context)
		{
			Reset();

			List<KCCProcessor> runtimeProcessors = context.Settings.RuntimeProcessors;
			for (int i = 0, count = runtimeProcessors.Count; i < count; ++i)
			{
				Processors.Add(runtimeProcessors[i]);
				ProcessorInfos.Add(KCCProcessorInfo.Default);
			}

			Frame frame = context.Frame;

			QList<KCCCollision> collisions = frame.ResolveList(context.KCC->Collisions);
			for (int i = 0, count = collisions.Count; i < count; ++i)
			{
				KCCCollision collision = collisions[i];
				if (KCCUtility.ResolveProcessor(frame, collision.Processor, out KCCProcessor processor) == true)
				{
					Processors.Add(processor);
					ProcessorInfos.Add(collision.GetProcessorInfo());
				}
			}

			QList<KCCModifier> modifiers = frame.ResolveList(context.KCC->Modifiers);
			for (int i = 0, count = modifiers.Count; i < count; ++i)
			{
				KCCModifier modifier = modifiers[i];
				if (KCCUtility.ResolveProcessor(frame, modifier.Processor, out KCCProcessor processor) == true)
				{
					Processors.Add(processor);
					ProcessorInfos.Add(modifier.GetProcessorInfo());
				}
			}
		}

		public void SuppressProcessor(KCCProcessor processor)
		{
			for (int i = 0, count = Processors.Count; i < count; ++i)
			{
				if (ReferenceEquals(Processors[i], processor) == true)
				{
					Processors[i]     = default;
					ProcessorInfos[i] = default;
				}
			}
		}
	}
}
