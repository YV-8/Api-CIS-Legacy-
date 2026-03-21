package com.user.management.services;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.Authentication;
import org.springframework.stereotype.Service;

import com.user.management.dtos.LoginRequestDTO;
import com.user.management.dtos.LoginResponseDTO;
import com.user.management.exceptions.UnauthorizedException;
import com.user.management.models.User;
import com.user.management.repository.UserRepository;
import com.user.management.utils.JwtUtil;

@Service
public class AuthService {

    @Autowired
    private JwtUtil jwtUtil;

    @Autowired
    private AuthenticationManager authManager;

    @Autowired
    private UserRepository userRepository;

    @Value("${jwt.expiration}")
    private long expiration;

    public LoginResponseDTO login(LoginRequestDTO dto) {

        Authentication authentication = authManager.authenticate(
                new UsernamePasswordAuthenticationToken(
                        dto.getLogin(),
                        dto.getPassword()));

        if (!authentication.isAuthenticated()) {
            throw new UnauthorizedException("Invalid credentials");
        }
        User user = userRepository.findByLogin(dto.getLogin())
                .orElseThrow(() -> new UnauthorizedException("User not found with login: " + dto.getLogin()));
        String token = jwtUtil.generateToken(user.getLogin(), user.getRole());

        return LoginResponseDTO.builder()
                .token(token)
                .type("Bearer")
                .expiresIn(expiration)
                .build();
    }

}
