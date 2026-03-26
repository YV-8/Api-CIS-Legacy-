package com.user.management.utils;package com.user.management.utils;














































}    }        assertFalse(jwtUtil.isTokenValid("invalid.token.here"));    void invalidToken_shouldBeInvalid() {    @Test    }        assertTrue(jwtUtil.isTokenExpired(token));        String token = jwtUtil.generateToken("user1", Role.USER);        ReflectionTestUtils.setField(jwtUtil, "expiration", -1000L);    void expiredToken_shouldBeMarkedExpired_andInvalidInExpiredCheck() {    @Test    }        assertFalse(jwtUtil.isTokenExpired(token));        assertTrue(jwtUtil.isTokenValid(token));        assertEquals(Role.USER, jwtUtil.extractRole(token));        assertEquals("user1", jwtUtil.extractLogin(token));        assertNotNull(token);        String token = jwtUtil.generateToken("user1", Role.USER);    void generateAndExtractClaims_shouldWork() {    @Test    }        ReflectionTestUtils.setField(jwtUtil, "expiration", 1000L);        ReflectionTestUtils.setField(jwtUtil, "secret", "01234567890123456789012345678901");        jwtUtil = new JwtUtil();    void setUp() {    @BeforeEach    private JwtUtil jwtUtil;class JwtUtilTest {import com.user.management.enums.Role;import org.springframework.test.util.ReflectionTestUtils;import org.junit.jupiter.api.Test;import org.junit.jupiter.api.BeforeEach;import static org.junit.jupiter.api.Assertions.*;
import static org.junit.jupiter.api.Assertions.*;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.springframework.test.util.ReflectionTestUtils;
import org.springframework.test.context.junit.jupiter.SpringExtension;

import com.user.management.enums.Role;

@ExtendWith(SpringExtension.class)
class JwtUtilTest {

    private JwtUtil jwtUtil;

    @BeforeEach
    void setUp() {
        jwtUtil = new JwtUtil();
        String secret = "01234567890123456789012345678901"; // 32 bytes key
        ReflectionTestUtils.setField(jwtUtil, "secret", secret);
        ReflectionTestUtils.setField(jwtUtil, "expiration", 3600000L); // 1 hour
    }

    @Test
    void generateToken_and_extractClaims_shouldWork() {
        String token = jwtUtil.generateToken("alice", Role.USER);

        assertNotNull(token);
        assertEquals("alice", jwtUtil.extractLogin(token));
        assertEquals(Role.USER, jwtUtil.extractRole(token));
        assertTrue(jwtUtil.isTokenValid(token));
        assertFalse(jwtUtil.isTokenExpired(token));
    }

    @Test
    void isTokenValid_invalidToken_shouldReturnFalse() {
        assertFalse(jwtUtil.isTokenValid("invalid.token.value"));
    }

    @Test
    void isTokenExpired_expiredToken_shouldReturnTrue() {
        ReflectionTestUtils.setField(jwtUtil, "expiration", -1000L);
        String token = jwtUtil.generateToken("bob", Role.ADMIN);

        assertTrue(jwtUtil.isTokenExpired(token));
    }
}
