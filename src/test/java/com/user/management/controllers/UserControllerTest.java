package com.user.management.controllers;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.user.management.dtos.UserRequestDTO;
import com.user.management.dtos.UserResponseDTO;
import com.user.management.dtos.UserUpdateRequestDTO;
import com.user.management.exceptions.ConflictException;
import com.user.management.exceptions.UserNotFoundException;
import com.user.management.services.UserService;
import com.user.management.utils.JwtUtil;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.boot.test.context.TestConfiguration;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Import;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.security.config.Customizer;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.test.context.TestPropertySource;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.web.client.HttpClientErrorException;

import java.util.List;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.doNothing;
import static org.mockito.Mockito.doThrow;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.delete;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.put;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@WebMvcTest(UserController.class)
@Import(UserControllerTest.TestSecurityConfig.class)
@TestPropertySource(properties = "security.enabled=false")
class UserControllerTest {

    @Autowired
    private MockMvc mockMvc;

    @Autowired
    private ObjectMapper objectMapper;

    @MockBean
    private UserService userService;

    @MockBean
    private JwtUtil jwtUtil;

    @TestConfiguration
    static class TestSecurityConfig {
        @Bean
        SecurityFilterChain testSecurityFilterChain(HttpSecurity http) throws Exception {
            http
                .csrf(csrf -> csrf.disable())
                .authorizeHttpRequests(auth -> auth.anyRequest().permitAll())
                .httpBasic(Customizer.withDefaults());
            return http.build();
        }
    }

    private UserRequestDTO buildValidRequest() {
        UserRequestDTO request = new UserRequestDTO();
        request.setName("David");
        request.setLogin("david");
        request.setPassword("1234");
        request.setRole("user");
        return request;
    }

    @Test
    void saveUser_shouldReturnCreated() throws Exception {
        UserResponseDTO response = new UserResponseDTO("1", "David", "david", "USER");
        when(userService.saveUser(any(UserRequestDTO.class))).thenReturn(response);

        mockMvc.perform(post("/v1/users")
                .contentType(MediaType.APPLICATION_JSON)
                .content(objectMapper.writeValueAsString(buildValidRequest())))
                .andExpect(status().isCreated());
    }

    @Test
    void getAllUsers_shouldReturnOk() throws Exception {
        when(userService.getAllUsers()).thenReturn(
                List.of(new UserResponseDTO("1", "David", "david", "USER")));

        mockMvc.perform(get("/v1/users"))
                .andExpect(status().isOk());
    }

    @Test
    void getUserById_shouldReturnOk() throws Exception {
        when(userService.getUserById("1"))
                .thenReturn(new UserResponseDTO("1", "David", "david", "USER"));

        mockMvc.perform(get("/v1/users/1"))
                .andExpect(status().isOk());
    }

    @Test
    void updateUser_shouldReturnOk() throws Exception {
        UserUpdateRequestDTO request = new UserUpdateRequestDTO();
        request.setName("David Updated");
        request.setLogin("david2");
        request.setPassword("5678");

        when(userService.updateUser(eq("1"), any(UserUpdateRequestDTO.class)))
                .thenReturn(new UserResponseDTO("1", "David Updated", "david2", "USER"));

        mockMvc.perform(put("/v1/users/1")
                .contentType(MediaType.APPLICATION_JSON)
                .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isOk());
    }

    @Test
    void deleteUser_shouldReturnNoContent() throws Exception {
        doNothing().when(userService).deleteUserById("1");

        mockMvc.perform(delete("/v1/users/1"))
                .andExpect(status().isNoContent());
    }

    @Test
    void saveUser_whenLoginAlreadyExists_shouldReturnConflict() throws Exception {
        when(userService.saveUser(any(UserRequestDTO.class)))
                .thenThrow(new DataIntegrityViolationException("Login already exists"));

        mockMvc.perform(post("/v1/users")
                .contentType(MediaType.APPLICATION_JSON)
                .content(objectMapper.writeValueAsString(buildValidRequest())))
                .andExpect(status().isConflict());
    }

    @Test
    void saveUser_whenConflictException_shouldReturnConflict() throws Exception {
        when(userService.saveUser(any(UserRequestDTO.class)))
                .thenThrow(new ConflictException("Login already exists"));

        mockMvc.perform(post("/v1/users")
                .contentType(MediaType.APPLICATION_JSON)
                .content(objectMapper.writeValueAsString(buildValidRequest())))
                .andExpect(status().isConflict());
    }

    @Test
    void saveUser_whenValidationFails_shouldReturnBadRequest() throws Exception {
        UserRequestDTO invalidRequest = new UserRequestDTO();

        mockMvc.perform(post("/v1/users")
                .contentType(MediaType.APPLICATION_JSON)
                .content(objectMapper.writeValueAsString(invalidRequest)))
                .andExpect(status().isBadRequest());
    }

    @Test
    void getUserById_whenNotFound_shouldReturnNotFound() throws Exception {
        when(userService.getUserById("99"))
                .thenThrow(new UserNotFoundException("User with id 99 was not found"));

        mockMvc.perform(get("/v1/users/99"))
                .andExpect(status().isNotFound());
    }

    @Test
    void updateUser_whenNotFound_shouldReturnNotFound() throws Exception {
        UserUpdateRequestDTO request = new UserUpdateRequestDTO();
        request.setName("David Updated");
        request.setLogin("david2");
        request.setPassword("5678");

        when(userService.updateUser(eq("99"), any(UserUpdateRequestDTO.class)))
                .thenThrow(new UserNotFoundException("User not found with id: 99"));

        mockMvc.perform(put("/v1/users/99")
                .contentType(MediaType.APPLICATION_JSON)
                .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isNotFound());
    }

    @Test
    void deleteUser_whenNotFound_shouldReturnNotFound() throws Exception {
        doThrow(new UserNotFoundException("User with id '99' not found"))
                .when(userService).deleteUserById("99");

        mockMvc.perform(delete("/v1/users/99"))
                .andExpect(status().isNotFound());
    }

    @Test
    void saveUser_whenForbidden_shouldReturnForbidden() throws Exception {
        when(userService.saveUser(any(UserRequestDTO.class)))
                .thenThrow(HttpClientErrorException.create(
                        HttpStatus.FORBIDDEN, "Forbidden", HttpHeaders.EMPTY, null, null));

        mockMvc.perform(post("/v1/users")
                .contentType(MediaType.APPLICATION_JSON)
                .content(objectMapper.writeValueAsString(buildValidRequest())))
                .andExpect(status().isForbidden());
    }

    @Test
    void getAllUsers_whenGenericException_shouldReturnInternalServerError() throws Exception {
        when(userService.getAllUsers()).thenThrow(new RuntimeException("Unexpected error"));

        mockMvc.perform(get("/v1/users"))
                .andExpect(status().isInternalServerError());
    }
}
