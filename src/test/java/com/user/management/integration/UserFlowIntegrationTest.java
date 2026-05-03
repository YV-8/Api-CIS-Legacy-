package com.user.management.integration;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.user.management.dtos.UserRequestDTO;
import com.user.management.dtos.UserUpdateRequestDTO;
import com.user.management.enums.Role;
import com.user.management.models.User;
import com.user.management.repository.UserRepositoryPort;
import com.user.management.utils.JwtUtil;
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

import java.util.HashSet;
import java.util.Set;

import static org.hamcrest.Matchers.greaterThanOrEqualTo;
import static org.hamcrest.Matchers.hasSize;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.*;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

/**
 * Integration tests for all user CRUD flows.
 * Covers: POST/GET/PUT/DELETE /v1/users
 * Tests security rules for each role: no token, USER, ADMIN.
 * Uses H2 in-memory DB (profile "test") with security enabled.
 */
@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.MOCK)
@AutoConfigureMockMvc
@ActiveProfiles("test")
class UserFlowIntegrationTest {

    @Autowired
    private MockMvc mockMvc;

    @Autowired
    private ObjectMapper objectMapper;

    @Autowired
    private UserRepositoryPort userRepository;

    @Autowired
    private PasswordEncoder passwordEncoder;

    @Autowired
    private JwtUtil jwtUtil;

    private static final String ADMIN_LOGIN         = "int_admin";
    private static final String USER_LOGIN          = "int_user";
    private static final String TO_DELETE_LOGIN     = "int_to_delete";
    private static final String EXTRA_CREATED_LOGIN = "int_admin_created";

    private User adminUser;
    private User regularUser;
    private String adminToken;
    private String userToken;

    private final Set<String> extraUserIds = new HashSet<>();

    @BeforeEach
    void setUp() {
        cleanupByLogin(ADMIN_LOGIN);
        cleanupByLogin(USER_LOGIN);
        cleanupByLogin(TO_DELETE_LOGIN);
        cleanupByLogin(EXTRA_CREATED_LOGIN);
        extraUserIds.clear();

        adminUser  = createUser("Admin Integration", ADMIN_LOGIN, "admin123", Role.ADMIN);
        adminToken = "Bearer " + jwtUtil.generateToken(adminUser.getLogin(), adminUser.getRole());

        regularUser = createUser("User Integration", USER_LOGIN, "user123", Role.USER);
        userToken   = "Bearer " + jwtUtil.generateToken(regularUser.getLogin(), regularUser.getRole());
    }

    @AfterEach
    void tearDown() {
        cleanupByLogin(ADMIN_LOGIN);
        cleanupByLogin(USER_LOGIN);
        cleanupByLogin(TO_DELETE_LOGIN);
        cleanupByLogin(EXTRA_CREATED_LOGIN);
        extraUserIds.forEach(id -> {
            if (userRepository.existsById(id)) {
                userRepository.deleteById(id);
            }
        });
    }

    // ─── Without token ────────────────────────────────────────────────────────

    @Test
    void getAllUsers_withoutToken_shouldReturn401() throws Exception {
        mockMvc.perform(get("/v1/users"))
                .andExpect(status().isUnauthorized());
    }

    @Test
    void createUser_withoutToken_shouldReturn401() throws Exception {
        mockMvc.perform(post("/v1/users")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(buildUserRequest("Ghost", "ghost_login", "USER"))))
                .andExpect(status().isUnauthorized());
    }

    @Test
    void getUserById_withoutToken_shouldReturn401() throws Exception {
        mockMvc.perform(get("/v1/users/{id}", adminUser.getId()))
                .andExpect(status().isUnauthorized());
    }

    @Test
    void updateUser_withoutToken_shouldReturn401() throws Exception {
        UserUpdateRequestDTO request = new UserUpdateRequestDTO();
        request.setName("Ghost Update");

        mockMvc.perform(put("/v1/users/{id}", adminUser.getId())
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isUnauthorized());
    }

    @Test
    void deleteUser_withoutToken_shouldReturn401() throws Exception {
        mockMvc.perform(delete("/v1/users/{id}", adminUser.getId()))
                .andExpect(status().isUnauthorized());
    }

    // ─── USER role ────────────────────────────────────────────────────────────

    @Test
    void getAllUsers_asUser_shouldReturn200WithList() throws Exception {
        mockMvc.perform(get("/v1/users")
                        .header("Authorization", userToken))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$", hasSize(greaterThanOrEqualTo(2))));
    }

    @Test
    void getUserById_asUser_shouldReturn200WithCorrectBody() throws Exception {
        mockMvc.perform(get("/v1/users/{id}", regularUser.getId())
                        .header("Authorization", userToken))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.id").value(regularUser.getId()))
                .andExpect(jsonPath("$.login").value(USER_LOGIN))
                .andExpect(jsonPath("$.name").value("User Integration"))
                .andExpect(jsonPath("$.role").value("USER"));
    }

    @Test
    void createUser_asUser_shouldReturn403() throws Exception {
        mockMvc.perform(post("/v1/users")
                        .header("Authorization", userToken)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(buildUserRequest("Forbidden", "forbidden_by_user", "USER"))))
                .andExpect(status().isForbidden());
    }

    @Test
    void deleteUser_asUser_shouldReturn403() throws Exception {
        mockMvc.perform(delete("/v1/users/{id}", regularUser.getId())
                        .header("Authorization", userToken))
                .andExpect(status().isForbidden());
    }

    @Test
    void updateUser_asUser_shouldReturn200WithUpdatedName() throws Exception {
        UserUpdateRequestDTO request = new UserUpdateRequestDTO();
        request.setName("Updated by User");

        mockMvc.perform(put("/v1/users/{id}", regularUser.getId())
                        .header("Authorization", userToken)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.id").value(regularUser.getId()))
                .andExpect(jsonPath("$.name").value("Updated by User"));
    }

    // ─── ADMIN role – happy path ──────────────────────────────────────────────

    @Test
    void createUser_asAdmin_shouldReturn201WithBody() throws Exception {
        UserRequestDTO request = buildUserRequest("Admin Created User", EXTRA_CREATED_LOGIN, "USER");

        String body = mockMvc.perform(post("/v1/users")
                        .header("Authorization", adminToken)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isCreated())
                .andExpect(jsonPath("$.id").isNotEmpty())
                .andExpect(jsonPath("$.login").value(EXTRA_CREATED_LOGIN))
                .andExpect(jsonPath("$.name").value("Admin Created User"))
                .andExpect(jsonPath("$.role").value("USER"))
                .andReturn().getResponse().getContentAsString();

        String id = objectMapper.readTree(body).get("id").asText();
        extraUserIds.add(id);
    }

    @Test
    void getAllUsers_asAdmin_shouldReturn200WithNonEmptyList() throws Exception {
        mockMvc.perform(get("/v1/users")
                        .header("Authorization", adminToken))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$", hasSize(greaterThanOrEqualTo(2))));
    }

    @Test
    void getUserById_asAdmin_shouldReturn200WithCorrectBody() throws Exception {
        mockMvc.perform(get("/v1/users/{id}", adminUser.getId())
                        .header("Authorization", adminToken))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.id").value(adminUser.getId()))
                .andExpect(jsonPath("$.login").value(ADMIN_LOGIN))
                .andExpect(jsonPath("$.role").value("ADMIN"));
    }

    @Test
    void updateUser_asAdmin_shouldReturn200WithUpdatedFields() throws Exception {
        UserUpdateRequestDTO request = new UserUpdateRequestDTO();
        request.setName("Admin Updated Name");
        request.setRole("OWNER");

        mockMvc.perform(put("/v1/users/{id}", regularUser.getId())
                        .header("Authorization", adminToken)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.name").value("Admin Updated Name"))
                .andExpect(jsonPath("$.role").value("OWNER"));
    }

    @Test
    void deleteUser_asAdmin_shouldReturn204() throws Exception {
        User toDelete = createUser("To Delete", TO_DELETE_LOGIN, "pass123", Role.USER);
        extraUserIds.add(toDelete.getId());

        mockMvc.perform(delete("/v1/users/{id}", toDelete.getId())
                        .header("Authorization", adminToken))
                .andExpect(status().isNoContent());
    }

    // ─── Error cases ──────────────────────────────────────────────────────────

    @Test
    void getUserById_withNonExistentId_shouldReturn404() throws Exception {
        mockMvc.perform(get("/v1/users/{id}", "non-existent-id-xyz-999")
                        .header("Authorization", adminToken))
                .andExpect(status().isNotFound())
                .andExpect(jsonPath("$.status").value(404));
    }

    @Test
    void createUser_withDuplicateLogin_shouldReturn409() throws Exception {
        UserRequestDTO duplicate = buildUserRequest("Duplicate", ADMIN_LOGIN, "USER");

        mockMvc.perform(post("/v1/users")
                        .header("Authorization", adminToken)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(duplicate)))
                .andExpect(status().isConflict())
                .andExpect(jsonPath("$.status").value(409));
    }

    @Test
    void createUser_withMissingRequiredFields_shouldReturn400() throws Exception {
        UserRequestDTO invalid = new UserRequestDTO();

        mockMvc.perform(post("/v1/users")
                        .header("Authorization", adminToken)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(invalid)))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.status").value(400));
    }

    @Test
    void updateUser_withNonExistentId_shouldReturn404() throws Exception {
        UserUpdateRequestDTO request = new UserUpdateRequestDTO();
        request.setName("Ghost Update");

        mockMvc.perform(put("/v1/users/{id}", "non-existent-id-xyz-999")
                        .header("Authorization", adminToken)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isNotFound())
                .andExpect(jsonPath("$.status").value(404));
    }

    @Test
    void deleteUser_withNonExistentId_shouldReturn404() throws Exception {
        mockMvc.perform(delete("/v1/users/{id}", "non-existent-id-xyz-999")
                        .header("Authorization", adminToken))
                .andExpect(status().isNotFound())
                .andExpect(jsonPath("$.status").value(404));
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private User createUser(String name, String login, String rawPassword, Role role) {
        User user = new User();
        user.setName(name);
        user.setLogin(login);
        user.setPassword(passwordEncoder.encode(rawPassword));
        user.setRole(role);
        return userRepository.save(user);
    }

    private UserRequestDTO buildUserRequest(String name, String login, String role) {
        UserRequestDTO dto = new UserRequestDTO();
        dto.setName(name);
        dto.setLogin(login);
        dto.setPassword("test_password_123");
        dto.setRole(role);
        return dto;
    }

    private void cleanupByLogin(String login) {
        userRepository.findByLogin(login)
                .ifPresent(u -> userRepository.deleteById(u.getId()));
    }
}
