using System;
using SCModManager.Avalonia.ViewModels;

namespace SCModManager.Avalonia.Utility
{
	public interface IShowWindow<TViewModel> where TViewModel : WindowViewModel
	{
		void Show();
	}

	public interface IShowWindow<TViewModel, T1> where TViewModel : WindowViewModel
	{
		void Show(T1 t1);
	}

	public interface IShowWindow<TViewModel, T1, T2> where TViewModel : WindowViewModel
	{
		void Show(T1 t1, T2 t2);
	}

	public interface IShowWindow<TViewModel, T1, T2, T3> where TViewModel : WindowViewModel
	{
		void Show(T1 t1, T2 t2, T3 t3);
	}

	public interface IShowWindow<TViewModel, T1, T2, T3, T4> where TViewModel : WindowViewModel
	{
		void Show(T1 t1, T2 t2, T3 t3, T4 t4);
	}

	public interface IShowWindow<TViewModel, T1, T2, T3, T4, T5> where TViewModel : WindowViewModel
	{
		void Show(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5);
	}

	public interface IShowWindow<TViewModel, T1, T2, T3, T4, T5, T6> where TViewModel : WindowViewModel
	{
		void Show(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6);
	}

	public interface IShowWindow<TViewModel, T1, T2, T3, T4, T5, T6, T7> where TViewModel : WindowViewModel
	{
		void Show(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7);
	}

	public interface IShowWindow<TViewModel, T1, T2, T3, T4, T5, T6, T7, T8> where TViewModel : WindowViewModel
	{
		void Show(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8);
	}
}
