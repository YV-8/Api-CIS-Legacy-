package com.user.management.integration;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.user.management.dtos.LoginRequestDTO;
import com.user.management.enums.Role;
import com.user.management.models.User;
import com.user.management.repository.UserRepositoryPort;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.http.MediaType;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.web.servlet.MockMvc;

import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

/**
 * Integration tests for the authentication flow.
 * Covers: POST /v1/auth/login
 * Uses H2 in-memory DB (profile "test") with security enabled.
 */
@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.MOCK)
@AutoConfigureMockMvc
@ActiveProfiles("test")
class AuthFlowIntegrationTest {

    @Autowired
    private MockMvc mockMvc;

    @Autowired
    private ObjectMapper objectMapper;

    @Autowired
    private UserRepositoryPort userRepository;

    @Autowired
    private PasswordEncoder passwordEncoder;

    private static final String TEST_LOGIN    = "auth_int_user";
    private static final String TEST_PASSWORD = "password123";

    @BeforeEach
    void setUp() {
        cleanupByLogin(TEST_LOGIN);

        User user = new User();
        user.setName("Auth Integration User");
        user.setLogin(TEST_LOGIN);
        user.setPassword(passwordEncoder.encode(TEST_PASSWORD));
        user.setRole(Role.USER);
        userRepository.save(user);
    }

    @AfterEach
    void tearDown() {
        cleanupByLogin(TEST_LOGIN);
    }

    // ─── Happy path ───────────────────────────────────────────────────────────

    @Test
    void login_withValidCredentials_shouldReturn200WithToken() throws Exception {
        LoginRequestDTO request = new LoginRequestDTO(TEST_LOGIN, TEST_PASSWORD);

        mockMvc.perform(post("/v1/auth/login")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.token").isNotEmpty())
                .andExpect(jsonPath("$.type").value("Bearer"))
                .andExpect(jsonPath("$.role").value("USER"))
                .andExpect(jsonPath("$.expiresIn").isNumber());
    }

    // ─── Error cases ──────────────────────────────────────────────────────────

    @Test
    void login_withWrongPassword_shouldReturn401() throws Exception {
        LoginRequestDTO request = new LoginRequestDTO(TEST_LOGIN, "wrong_password");

        mockMvc.perform(post("/v1/auth/login")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isUnauthorized());
    }

    @Test
    void login_withNonExistentUser_shouldReturn401() throws Exception {
        LoginRequestDTO request = new LoginRequestDTO("user_that_does_not_exist", "somepassword");

        mockMvc.perform(post("/v1/auth/login")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isUnauthorized());
    }

    @Test
    void login_withBlankLoginAndPassword_shouldReturn400() throws Exception {
        LoginRequestDTO request = new LoginRequestDTO("", "");

        mockMvc.perform(post("/v1/auth/login")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.status").value(400));
    }

    @Test
    void login_withBlankPasswordOnly_shouldReturn400() throws Exception {
        LoginRequestDTO request = new LoginRequestDTO(TEST_LOGIN, "");

        mockMvc.perform(post("/v1/auth/login")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.status").value(400));
    }

    @Test
    void login_withBlankLoginOnly_shouldReturn400() throws Exception {
        LoginRequestDTO request = new LoginRequestDTO("", TEST_PASSWORD);

        mockMvc.perform(post("/v1/auth/login")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.status").value(400));
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    private void cleanupByLogin(String login) {
        userRepository.findByLogin(login)
                .ifPresent(u -> userRepository.deleteById(u.getId()));
    }
}
