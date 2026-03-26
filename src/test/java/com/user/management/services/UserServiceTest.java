package com.user.management.services;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.*;

import java.util.List;
import java.util.Optional;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.modelmapper.ModelMapper;
import org.springframework.security.crypto.password.PasswordEncoder;

import com.user.management.dtos.UserRequestDTO;
import com.user.management.dtos.UserResponseDTO;
import com.user.management.dtos.UserUpdateRequestDTO;
import com.user.management.enums.Role;
import com.user.management.exceptions.UserNotFoundException;
import com.user.management.models.User;
import com.user.management.repository.UserRepository;

@ExtendWith(MockitoExtension.class)
class UserServiceTest {

    @Mock
    private ModelMapper modelMapper;

    @Mock
    private UserRepository userRepository;

    @Mock
    private PasswordEncoder passwordEncoder;

    @InjectMocks
    private UserService userService;

    private UserRequestDTO request;
    private UserUpdateRequestDTO updateRequest;

    @BeforeEach
    void setUp() {
        request = new UserRequestDTO();
        request.setName("Alice");
        request.setLogin("alice");
        request.setPassword("1234");
        request.setRole("user");

        updateRequest = new UserUpdateRequestDTO();
        updateRequest.setName("AliceUpdated");
        updateRequest.setLogin("aliceUpdated");
        updateRequest.setPassword("4321");
        updateRequest.setRole("admin");
    }

    @Test
    void saveUser_shouldReturnSavedResponse() {
        User userEntity = new User();
        userEntity.setName(request.getName());
        userEntity.setLogin(request.getLogin());
        userEntity.setPassword(request.getPassword());

        User savedUser = new User("id-1", request.getName(), request.getLogin(), "encodedPass", Role.USER);
        UserResponseDTO savedDto = new UserResponseDTO("id-1", request.getName(), request.getLogin(), Role.USER.getValue());

        when(modelMapper.map(request, User.class)).thenReturn(userEntity);
        when(passwordEncoder.encode(request.getPassword())).thenReturn("encodedPass");
        when(userRepository.save(userEntity)).thenReturn(savedUser);
        when(modelMapper.map(savedUser, UserResponseDTO.class)).thenReturn(savedDto);

        UserResponseDTO result = userService.saveUser(request);

        assertNotNull(result);
        assertEquals("id-1", result.getId());
        assertEquals("Alice", result.getName());
        assertEquals("alice", result.getLogin());
        assertEquals("USER", result.getRole());

        verify(userRepository, times(1)).save(userEntity);
    }

    @Test
    void getAllUsers_shouldReturnMappedResponseList() {
        User user = new User("id-1", "Alice", "alice", "x", Role.USER);
        UserResponseDTO dto = new UserResponseDTO("id-1", "Alice", "alice", "USER");

        when(userRepository.findAll()).thenReturn(List.of(user));
        when(modelMapper.map(user, UserResponseDTO.class)).thenReturn(dto);

        List<UserResponseDTO> result = userService.getAllUsers();

        assertEquals(1, result.size());
        assertEquals("id-1", result.get(0).getId());
        assertEquals("USER", result.get(0).getRole());
        verify(userRepository, times(1)).findAll();
    }

    @Test
    void getUserById_existing_shouldReturnResponse() {
        User user = new User("id-2", "Bob", "bob", "x", Role.OWNER);
        UserResponseDTO dto = new UserResponseDTO("id-2", "Bob", "bob", "OWNER");

        when(userRepository.findById("id-2")).thenReturn(Optional.of(user));
        when(modelMapper.map(user, UserResponseDTO.class)).thenReturn(dto);

        UserResponseDTO result = userService.getUserById("id-2");

        assertNotNull(result);
        assertEquals("bob", result.getLogin());
        assertEquals("OWNER", result.getRole());
    }

    @Test
    void getUserById_notFound_shouldThrow() {
        when(userRepository.findById("x")).thenReturn(Optional.empty());
        assertThrows(UserNotFoundException.class, () -> userService.getUserById("x"));
    }

    @Test
    void updateUser_allFieldsUpdated_shouldReturnUpdatedResponse() {
        User existing = new User("id-3", "Alice", "alice", "oldPass", Role.USER);
        User saved = new User("id-3", "AliceUpdated", "aliceUpdated", "encoded4321", Role.ADMIN);
        UserResponseDTO response = new UserResponseDTO("id-3", "AliceUpdated", "aliceUpdated", "ADMIN");

        when(userRepository.findById("id-3")).thenReturn(Optional.of(existing));
        when(passwordEncoder.encode(updateRequest.getPassword())).thenReturn("encoded4321");
        when(userRepository.save(existing)).thenReturn(saved);
        when(modelMapper.map(saved, UserResponseDTO.class)).thenReturn(response);

        UserResponseDTO result = userService.updateUser("id-3", updateRequest);

        assertEquals("AliceUpdated", result.getName());
        assertEquals("aliceUpdated", result.getLogin());
        assertEquals("ADMIN", result.getRole());

        verify(userRepository).save(existing);
    }

    @Test
    void deleteUserById_existing_shouldDelete() {
        when(userRepository.existsById("id-4")).thenReturn(true);

        assertDoesNotThrow(() -> userService.deleteUserById("id-4"));

        verify(userRepository).deleteById("id-4");
    }

    @Test
    void deleteUserById_missing_shouldThrow() {
        when(userRepository.existsById("id-unknown")).thenReturn(false);
        assertThrows(UserNotFoundException.class, () -> userService.deleteUserById("id-unknown"));
    }

    @Test
    void loadUserByUsername_existing_shouldReturnSecurityUser() {
        User user = new User("id-5", "Carlos", "carlos", "pw", Role.ADMIN);
        when(userRepository.findByLogin("carlos")).thenReturn(Optional.of(user));

        var userDetails = userService.loadUserByUsername("carlos");

        assertNotNull(userDetails);
        assertEquals("carlos", userDetails.getUsername());
        assertTrue(userDetails.getAuthorities().stream().anyMatch(ga -> ga.getAuthority().equals("ROLE_ADMIN")));
    }

    @Test
    void loadUserByUsername_missing_shouldThrow() {
        when(userRepository.findByLogin("nope")).thenReturn(Optional.empty());
        assertThrows(org.springframework.security.core.userdetails.UsernameNotFoundException.class,
                () -> userService.loadUserByUsername("nope"));
    }
}
