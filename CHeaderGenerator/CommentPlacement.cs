using System.ComponentModel;

namespace CHeaderGenerator
{
	public enum CommentPlacement
	{
		[Description("Inside Include Guard")]
		InsideIncludeGuard,

		[Description("Start of File (Outside Include Guard)")]
		StartOfFile
	}
}