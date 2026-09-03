# Security Policy

## Supported Versions

Currently, the following versions of the Görev Takip Sistemi are being supported with security updates.

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |
| < 1.0   | :x:                |

## Reporting a Vulnerability

Security is a top priority for us. If you discover a security vulnerability within this project, please follow these steps to report it:

1. **Do not open a public issue.** This ensures that the vulnerability is not exploited before it can be fixed.
2. Please use the private vulnerability reporting feature on GitHub or send an email to the repository administrators.
3. Provide a detailed description of the vulnerability, including:
   - The type of vulnerability (e.g., XSS, SQLi, CSRF, Mass Assignment, Data Leak).
   - Step-by-step instructions to reproduce the issue.
   - The potential impact of the vulnerability.

We will review the report and respond as quickly as possible. If the vulnerability is confirmed, we will work on a patch and release a security update.

## Best Practices Implemented

This project already incorporates several security best practices:
*   **HttpOnly, Secure Cookies:** JWT tokens are stored securely in cookies to prevent XSS attacks.
*   **Role-Based Access Control (RBAC):** Endpoints are protected by strict role validations.
*   **Rate Limiting:** Protection against Brute-Force and DDoS attacks.
*   **Mass Assignment Protection:** Data transfer objects (DTOs) and record structures prevent unauthorized property assignments.
*   **Whitelist File Validation:** Uploads are strictly validated against a safe extensions list.

Thank you for helping us keep Görev Takip Sistemi secure!
