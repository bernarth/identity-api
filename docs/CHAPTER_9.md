# Chapter 9

In this chapter we implemented rate limiting and security hardening.

## Questions

1. What is a brute-force attack on a login endpoint and how does rate limiting mitigate it?

Rate limiting caps the attempts per attacker per time window, turning "thousands of guesses per second" into 5 per minute. So, even a weak 8-character password takes centuries to guess.
Combined with the lockout the attacker hits two indepedent walls.

Note: a DoS/DDoS protection, a distributed attack comes from thousand of IPs, each staying under its own 5/min bucket and that's handled at infrastructure layer.

2. What is the difference between a fixed window limiter and a sliding window limiter?

- Fixed window: time is chopped into a rigid blocks. The counter resets to zero at each boundary.
- Sliding window: the limit applies to the trailing 60 seconds from each request, so that burst is impossible. Those first 5 still count against the window when the next 5 arrive.

3. Why should secrets never be committed to version control, even in a private repository?

Because, if the code is stolen the secrets could be compromised. Also if the repository is private but one day becomes public anyone could check the git history.

4. What HTTP status code does rate limiting return and what does it mean?

The status code is 429 and it means there are too many requests.

5. What other security headers or middlewares would you add to a production auth API?

- HTTPS enforced + HSTS (`app.UseHsts()`). Tokens travel in headers/bodies. Plaintext HTTP would leak them. HSTS makes browsers refuse to ever downgrade.
- `ForwardedHeaders` middleware. behind a reverse proxy, `RemoteIpAddress` is the proxy, so your per-ip limiter collapses into one shared bucket until you trust X-Forwarded-For.
- Request body size limits: an auth request is tiny. A 100 MB POST to `/login` should die early.
