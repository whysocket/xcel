﻿using Xcel.Services.Email.Interfaces;

namespace Xcel.Services.Email.Templates;

public record TutorCvRejectionEmail(
    string FullName,
    string? RejectionReason
) : IEmail
{
    public string Subject => "Your application was rejected";
}