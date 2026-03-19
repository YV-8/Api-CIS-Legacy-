package com.user.management.controllers;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.user.management.dtos.UserRequestDTO;
import com.user.management.dtos.UserResponseDTO;
import com.user.management.services.UserService;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.boot.test.context.TestConfiguration;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Import;
import org.springframework.http.MediaType;
import org.springframework.security.config.Customizer;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.test.web.servlet.MockMvc;

import java.util.List;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.doNothing;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.delete;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.put;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@WebMvcTest(UserController.class)
@Import(UserControllerTest.TestSecurityConfig.class)
class UserControllerTest {

    @Autowired
    private MockMvc mockMvc;

    @Autowired
    private ObjectMapper objectMapper;

    @MockBean
    private UserService userService;

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

    @Test
    void saveUser_shouldReturnCreated() throws Exception {
        UserRequestDTO request = new UserRequestDTO();
        request.setName("David");
        request.setLogin("david");
        request.setPassword("1234");

        UserResponseDTO response = new UserResponseDTO("1", "David", "david");

        when(userService.saveUser(any(UserRequestDTO.class))).thenReturn(response);

        mockMvc.perform(post("/v1/users")
                .contentType(MediaType.APPLICATION_JSON)
                .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isCreated());
    }

    @Test
    void getAllUsers_shouldReturnOk() throws Exception {
        when(userService.getAllUsers()).thenReturn(
                List.of(new UserResponseDTO("1", "David", "david"))
        );

        mockMvc.perform(get("/v1/users"))
                .andExpect(status().isOk());
    }

    @Test
    void getUserById_shouldReturnOk() throws Exception {
        when(userService.getUserById("1"))
                .thenReturn(new UserResponseDTO("1", "David", "david"));

        mockMvc.perform(get("/v1/users/1"))
                .andExpect(status().isOk());
    }

    @Test
    void updateUser_shouldReturnOk() throws Exception {
        UserRequestDTO request = new UserRequestDTO();
        request.setName("David Updated");
        request.setLogin("david2");
        request.setPassword("5678");

        when(userService.updateUser(eq("1"), any(UserRequestDTO.class)))
                .thenReturn(new UserResponseDTO("1", "David Updated", "david2"));

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
}