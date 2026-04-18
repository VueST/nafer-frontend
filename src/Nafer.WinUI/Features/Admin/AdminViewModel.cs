using Nafer.Core.Application.Common;
using Nafer.Core.Domain.Attributes;
using Nafer.Core.Domain.Models;

namespace Nafer.WinUI.Features.Admin;

[Authorize(UserRole.Admin)]
public class AdminViewModel : ViewModelBase
{
}
