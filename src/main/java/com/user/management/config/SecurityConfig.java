package com.user.management.config;

import com.fasterxml.jackson.databind.ObjectMapper;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Lazy;
import org.springframework.http.MediaType;

import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.AuthenticationProvider;
import org.springframework.security.authentication.dao.DaoAuthenticationProvider;

import org.springframework.security.config.annotation.authentication.configuration.AuthenticationConfiguration;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;

import org.springframework.security.config.http.SessionCreationPolicy;

import org.springframework.security.core.userdetails.UserDetailsService;

import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.security.crypto.password.PasswordEncoder;

import org.springframework.security.web.SecurityFilterChain;

import jakarta.servlet.http.HttpServletResponse;

import java.util.Map;

@Configuration
public class SecurityConfig {

    private final UserDetailsService userDetailsService;

    public SecurityConfig(@Lazy UserDetailsService userDetailsService) {
        this.userDetailsService = userDetailsService;
    }

    @Bean
    public SecurityFilterChain securityFilterChain(HttpSecurity http) throws Exception {

        http
            // CSRF OFF (solo porque usamos JWT)
            .csrf(csrf -> csrf.disable())

            // CORS (activar si frontend separado)
            .cors(cors -> {})

            // Manejo de errores centralizado
            .exceptionHandling(exception -> exception
                .authenticationEntryPoint((request, response, ex) -> {
                    buildErrorResponse(response, HttpServletResponse.SC_UNAUTHORIZED,
                            "Unauthorized", "Token missing or invalid");
                })
                .accessDeniedHandler((request, response, ex) -> {
                    buildErrorResponse(response, HttpServletResponse.SC_FORBIDDEN,
                            "Forbidden", "You do not have permission");
                })
            )

            // Stateless JWT
            .sessionManagement(session ->
                session.sessionCreationPolicy(SessionCreationPolicy.STATELESS)
            )

            // Autorización
            .authorizeHttpRequests(auth -> auth
                .requestMatchers("/auth/**").permitAll()
                //.requestMatchers(HttpMethod.POST, "/users").permitAll()
                .requestMatchers("/swagger-ui/**", "/v3/api-docs/**").permitAll()
                .anyRequest().authenticated()
            )

            // Provider
            .authenticationProvider(authenticationProvider());

            // JWT Filter (CLAVE)
            //.addFilterBefore(jwtAuthenticationFilter, UsernamePasswordAuthenticationFilter.class);

        return http.build();
    }

    // 🔧 Helper para respuestas JSON
    private void buildErrorResponse(HttpServletResponse response, int status, String error, String message)
            throws java.io.IOException {

        response.setStatus(status);
        response.setContentType(MediaType.APPLICATION_JSON_VALUE);

        Map<String, String> body = Map.of(
                "error", error,
                "message", message
        );

        new ObjectMapper().writeValue(response.getOutputStream(), body);
    }

    // Password encoder
    @Bean
    public PasswordEncoder passwordEncoder() {
        return new BCryptPasswordEncoder();
    }

    // Authentication provider
    @Bean
    public AuthenticationProvider authenticationProvider() {
        DaoAuthenticationProvider provider = new DaoAuthenticationProvider();
        provider.setUserDetailsService(userDetailsService);
        provider.setPasswordEncoder(passwordEncoder());
        return provider;
    }

    // Authentication manager
    @Bean
    public AuthenticationManager authenticationManager(AuthenticationConfiguration config) throws Exception {
        return config.getAuthenticationManager();
    }
}