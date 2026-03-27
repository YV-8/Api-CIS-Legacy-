package com.user.management.services;

import java.util.List;
import java.util.Optional;

import org.modelmapper.ModelMapper;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.security.core.authority.SimpleGrantedAuthority;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.security.core.userdetails.UserDetailsService;
import org.springframework.security.core.userdetails.UsernameNotFoundException;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import com.user.management.dtos.UserRequestDTO;
import com.user.management.dtos.UserResponseDTO;
import com.user.management.dtos.UserUpdateRequestDTO;
import com.user.management.enums.Role;
import com.user.management.exceptions.UserNotFoundException;
import com.user.management.models.User;
import com.user.management.repository.UserRepository;

import lombok.AllArgsConstructor;

@Service
@AllArgsConstructor
public class UserService implements UserDetailsService {

    private final ModelMapper modelMapper;
    private final UserRepository userRepository;  
    private final PasswordEncoder passwordEncoder;

    @Transactional
    public UserResponseDTO saveUser(UserRequestDTO user) {
        User userEntity = modelMapper.map(user, User.class);
        userEntity.setRole(Role.valueOf(user.getRole().toUpperCase()));
        userEntity.setPassword(passwordEncoder.encode(user.getPassword()));

        User savedUser = userRepository.save(userEntity);
        UserResponseDTO responseDTO = modelMapper.map(savedUser, UserResponseDTO.class);
        responseDTO.setRole(savedUser.getRole().getValue());
        return responseDTO;
    }

    public List<UserResponseDTO> getAllUsers() {
        return userRepository.findAll()
                .stream()
                .map(user -> 
                    {
                        UserResponseDTO dto = modelMapper.map(user, UserResponseDTO.class);
                        dto.setRole(user.getRole().getValue());
                        return dto;
                    }
                )
                .toList();
    }

    public UserResponseDTO getUserById(String id) {
        User user = userRepository.findById(id)
                .orElseThrow(() -> new UserNotFoundException("User with id " + id + " was not found"));
        UserResponseDTO responseDTO = modelMapper.map(user, UserResponseDTO.class);
        responseDTO.setRole(user.getRole().getValue());
        return responseDTO;
    }

    @Transactional
    public UserResponseDTO updateUser(String id, UserUpdateRequestDTO request) {
        User user = userRepository.findById(id)
                .orElseThrow(() -> new UserNotFoundException("User not found with id: " + id));

        if (request.getName() != null && !request.getName().isBlank()) {
            user.setName(request.getName());
        }

        if (request.getLogin() != null && !request.getLogin().isBlank()) {
            user.setLogin(request.getLogin());
        }

        if (request.getPassword() != null && !request.getPassword().isBlank()) {
            user.setPassword(passwordEncoder.encode(request.getPassword()));
        }

        if(request.getRole() != null && !request.getRole().isBlank()) {
            user.setRole(Role.valueOf(request.getRole().toUpperCase()));
        }

        User updatedUser = userRepository.save(user);
        UserResponseDTO responseDTO = modelMapper.map(updatedUser, UserResponseDTO.class);
        responseDTO.setRole(updatedUser.getRole().getValue());
        return responseDTO;
    }

    @Transactional
    public void deleteUserById(String id) {
        if (!userRepository.existsById(id)) {
            throw new UserNotFoundException("User with id '" + id + "' not found");
        }
        userRepository.deleteById(id);
    }

    @Override
    public UserDetails loadUserByUsername(String username) throws UsernameNotFoundException {

        Optional<User> userOptional = userRepository.findByLogin(username);

        if (userOptional.isEmpty()) {
            throw new UsernameNotFoundException("User not found with login: " + username);
        }

        User user = userOptional.get();

        return org.springframework.security.core.userdetails.User.builder()
                .username(user.getLogin())
                .password(user.getPassword())
                .authorities(List.of(new SimpleGrantedAuthority("ROLE_"+user.getRole().name())))
                .build();
    }
}