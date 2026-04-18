using Nafer.Core.Application.Common;
using Nafer.Core.Domain.Attributes;
using Nafer.Core.Domain.Models;

namespace Nafer.WinUI.Features.Moderation;

[Authorize(UserRole.Mod)]
public class ModerationViewModel : ViewModelBase
{
}
