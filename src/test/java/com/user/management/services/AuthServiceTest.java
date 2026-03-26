package com.user.management.services;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.*;

import java.util.Optional;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.Authentication;

import com.user.management.dtos.LoginRequestDTO;
import com.user.management.dtos.LoginResponseDTO;
import com.user.management.enums.Role;
import com.user.management.exceptions.UnauthorizedException;
import com.user.management.models.User;
import com.user.management.repository.UserRepository;
import com.user.management.utils.JwtUtil;

@ExtendWith(MockitoExtension.class)
class AuthServiceTest {

    @Mock
    private AuthenticationManager authenticationManager;

    @Mock
    private JwtUtil jwtUtil;

    @Mock
    private UserRepository userRepository;

    @InjectMocks
    private AuthService authService;

    @Test
    void login_validCredentials_shouldReturnTokenData() {
        LoginRequestDTO request = new LoginRequestDTO("alice", "1234");

        Authentication authentication = mock(Authentication.class);
        when(authentication.isAuthenticated()).thenReturn(true);

        User user = new User("id", "Alice", "alice", "encoded", Role.USER);

        when(authenticationManager.authenticate(any(UsernamePasswordAuthenticationToken.class))).thenReturn(authentication);
        when(userRepository.findByLogin("alice")).thenReturn(Optional.of(user));
        when(jwtUtil.generateToken("alice", Role.USER)).thenReturn("jwt-token-123");

        LoginResponseDTO response = authService.login(request);

        assertNotNull(response);
        assertEquals("jwt-token-123", response.getToken());
        assertEquals("Bearer", response.getType());
        assertEquals(Role.USER.getValue(), response.getRole());
        assertTrue(response.getExpiresIn() > 0);
    }

    @Test
    void login_notAuthenticated_shouldThrowUnauthorized() {
        LoginRequestDTO request = new LoginRequestDTO("bob", "1234");

        Authentication authentication = mock(Authentication.class);
        when(authentication.isAuthenticated()).thenReturn(false);

        when(authenticationManager.authenticate(any(UsernamePasswordAuthenticationToken.class))).thenReturn(authentication);

        assertThrows(UnauthorizedException.class, () -> authService.login(request));
    }

    @Test
    void login_userNotFound_shouldThrowUnauthorized() {
        LoginRequestDTO request = new LoginRequestDTO("bob", "1234");

        Authentication authentication = mock(Authentication.class);
        when(authentication.isAuthenticated()).thenReturn(true);

        when(authenticationManager.authenticate(any(UsernamePasswordAuthenticationToken.class))).thenReturn(authentication);
        when(userRepository.findByLogin("bob")).thenReturn(Optional.empty());

        assertThrows(UnauthorizedException.class, () -> authService.login(request));
    }
}
