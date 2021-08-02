# Security Policy

## Reporting a Vulnerability

The .NET Outbox Pattern project takes security seriously. We appreciate your efforts to responsibly disclose security vulnerabilities to us.

### DO NOT Open Public Issues for Security Vulnerabilities

**Please do not open a GitHub issue to report security vulnerabilities.** Public issues can expose security risks to bad actors. Instead, use the procedures below.

## Responsible Disclosure

### Option 1: GitHub Private Vulnerability Reporting (Recommended)

GitHub provides a secure way to report vulnerabilities privately:

1. Visit: https://github.com/sarmkadan/dotnet-outbox-pattern/security/advisories/new
2. Use the GitHub interface to report the vulnerability
3. Your report will be visible only to the maintainers

### Option 2: Email

If you prefer email, send vulnerability details to:

```
rutova2@gmail.com
```

Include:
- Description of the vulnerability
- Steps to reproduce (if applicable)
- Potential impact
- Suggested fix (if you have one)
- Your name and contact information (optional)

## Response Timeline

We are committed to addressing security vulnerabilities promptly:

- **Acknowledgment**: We will acknowledge receipt of your report within **48 hours**
- **Assessment**: We will assess the vulnerability within **1 week**
- **Fix**: We will work on a fix and coordinate a responsible disclosure timeline with you
- **Publication**: Once fixed, we will publish a security advisory

## Supported Versions

Security updates are provided for:

| Version | Supported |
|---------|-----------|
| 1.x     | ✅ Yes    |
| 0.x     | ❌ No     |

Only the latest version receives security updates. We recommend upgrading to the latest version to receive all security patches.

## Types of Vulnerabilities We Prioritize

We prioritize fixes for:

- **Authentication & Authorization**: Issues that could allow unauthorized access
- **Data Exposure**: Vulnerabilities that could leak sensitive data
- **Code Injection**: SQL injection, command injection, code injection attacks
- **Cryptography**: Weak encryption, insecure algorithms
- **Input Validation**: Issues with message/payload validation
- **Dependency Vulnerabilities**: Known vulnerabilities in third-party packages

## Security Best Practices for Users

When using the .NET Outbox Pattern in production:

1. **Keep Dependencies Updated**: Regularly update NuGet packages
   ```bash
   dotnet outdated
   dotnet package update
   ```

2. **Use HTTPS**: Always use HTTPS in production environments

3. **Database Security**:
   - Use strong credentials
   - Restrict database access to only required services
   - Enable encryption at rest and in transit
   - Use parameterized queries (already enforced in this library)

4. **Configuration**:
   - Never hardcode secrets
   - Use environment variables or secure configuration managers
   - Rotate credentials regularly
   - Audit configuration changes

5. **Monitoring**:
   - Monitor for failed message deliveries
   - Set up alerts for dead letter queue growth
   - Review logs regularly for suspicious activity
   - Track message latency and throughput

6. **Access Control**:
   - Restrict API endpoints to authorized consumers
   - Implement rate limiting
   - Use API keys or tokens for authentication
   - Log all access attempts

## Known Issues

There are currently no known unpatched security vulnerabilities in the .NET Outbox Pattern.

## Security Contact

For all security-related inquiries, use the channels listed in the "Reporting a Vulnerability" section above.

## Acknowledgments

We thank all researchers and users who report security vulnerabilities responsibly, helping us keep this project secure for everyone.

## Additional Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [GitHub Security Advisory](https://docs.github.com/en/code-security/advisory/github-security-advisory)
- [Microsoft .NET Security Best Practices](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/security)