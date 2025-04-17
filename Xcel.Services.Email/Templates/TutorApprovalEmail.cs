﻿using Xcel.Services.Email.Interfaces;

namespace Xcel.Services.Email.Templates;

public record TutorApprovalEmail(
    string FullName
) : IEmail
{
    public string Subject => "Your CV has been approved. Please select the proposed dates";
}